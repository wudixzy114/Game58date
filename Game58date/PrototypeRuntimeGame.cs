#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Game58date.Gameplay;
using Stride.Core.Serialization.Contents;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Game58date.Terrain;

namespace Game58date;

public sealed class PrototypeRuntimeGame : Game
{
    public static RuntimeLaunchTarget? PendingLaunchTarget { get; set; }

    private TimeSpan fullscreenAttemptDelay = TimeSpan.FromMilliseconds(150);
    private bool hasAppliedFullscreen;
    private Int2 lastWindowedSize = new(1280, 720);

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        ConfigureWindowForGameplay();
        if (IsDevRouterSceneActive())
        {
            return;
        }

        RuntimeMode runtimeMode = ResolveRuntimeMode();
        if (runtimeMode == RuntimeMode.UiShowcase)
        {
            LoadUiShowcaseScene();
            return;
        }

        var scene = SceneSystem.SceneInstance.RootScene;
        if (scene is null)
        {
            scene = new Scene();
            SceneSystem.SceneInstance.RootScene = scene;
        }

        string runtimeAnchorName = runtimeMode == RuntimeMode.Prototype
            ? "LegacyPrototypeRuntime"
            : "PrototypeRuntime";
        var anchor = scene.Entities.FirstOrDefault(entity => entity.Name == runtimeAnchorName);
        if (anchor is null)
        {
            anchor = new Entity(runtimeAnchorName);
            if (runtimeMode == RuntimeMode.Prototype)
            {
                anchor.Add(new HeroJourneyPrototypeScript());
            }
            else
            {
                anchor.Add(new VoxelTerrainRuntimeScript());
            }

            scene.Entities.Add(anchor);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (!hasAppliedFullscreen)
        {
            fullscreenAttemptDelay -= gameTime.Elapsed;
            if (fullscreenAttemptDelay <= TimeSpan.Zero && Window.Visible && !Window.IsMinimized)
            {
                Window.PreferredFullscreenSize = GetPreferredFullscreenSize();
                Window.IsBorderLess = true;
                Window.FullscreenIsBorderlessWindow = true;
                Window.IsFullscreen = true;
                hasAppliedFullscreen = true;
            }
        }

        SynchronizePresenterWithWindow();
        base.Update(gameTime);
    }

    private void ConfigureWindowForGameplay()
    {
        Window.AllowUserResizing = false;
        Window.IsBorderLess = false;
        Window.FullscreenIsBorderlessWindow = true;
        Window.IsFullscreen = false;

        Rectangle bounds = Window.ClientBounds;
        Int2 currentSize = new(Math.Max(bounds.Width, 1), Math.Max(bounds.Height, 1));
        lastWindowedSize = currentSize;
        Window.PreferredWindowedSize = currentSize;
        Window.PreferredFullscreenSize = GetPreferredFullscreenSize();

        IsMouseVisible = false;
    }

    private void SynchronizePresenterWithWindow()
    {
        if (Window.IsMinimized || !Window.Visible || GraphicsDevice?.Presenter?.BackBuffer is null)
        {
            return;
        }

        Int2 targetSize = Window.IsFullscreen
            ? GetPreferredFullscreenSize()
            : GetCurrentClientSize();

        if (Window.IsFullscreen)
        {
            if (Window.PreferredFullscreenSize.X != targetSize.X || Window.PreferredFullscreenSize.Y != targetSize.Y)
            {
                Window.PreferredFullscreenSize = targetSize;
            }
        }
        else
        {
            lastWindowedSize = targetSize;
            if (Window.PreferredWindowedSize.X != targetSize.X || Window.PreferredWindowedSize.Y != targetSize.Y)
            {
                Window.PreferredWindowedSize = targetSize;
            }
        }

        var presenter = GraphicsDevice.Presenter;
        var backBuffer = presenter.BackBuffer;
        if (backBuffer.Width == targetSize.X && backBuffer.Height == targetSize.Y)
        {
            return;
        }

        presenter.Resize(targetSize.X, targetSize.Y, presenter.Description.BackBufferFormat);
    }

    private Int2 GetPreferredFullscreenSize()
    {
        Int2 outputSize = TryGetTargetOutputSize();
        if (outputSize.X > 0 && outputSize.Y > 0)
        {
            return outputSize;
        }

        return lastWindowedSize;
    }

    private Int2 TryGetTargetOutputSize()
    {
        if (GraphicsDevice?.Adapter is null)
        {
            return default;
        }

        Int2 clientSize = GetCurrentClientSize();
        Int2 windowPosition = Window.Position;
        int centerX = windowPosition.X + clientSize.X / 2;
        int centerY = windowPosition.Y + clientSize.Y / 2;

        GraphicsOutput? selectedOutput = null;
        foreach (var output in GraphicsDevice.Adapter.Outputs)
        {
            selectedOutput ??= output;

            Rectangle bounds = output.DesktopBounds;
            if (centerX >= bounds.X &&
                centerX < bounds.X + bounds.Width &&
                centerY >= bounds.Y &&
                centerY < bounds.Y + bounds.Height)
            {
                selectedOutput = output;
                break;
            }
        }

        if (selectedOutput is null)
        {
            return default;
        }

        int width = selectedOutput.CurrentDisplayMode.Width > 0
            ? selectedOutput.CurrentDisplayMode.Width
            : selectedOutput.DesktopBounds.Width;
        int height = selectedOutput.CurrentDisplayMode.Height > 0
            ? selectedOutput.CurrentDisplayMode.Height
            : selectedOutput.DesktopBounds.Height;

        return new Int2(Math.Max(width, 1), Math.Max(height, 1));
    }

    private Int2 GetCurrentClientSize()
    {
        Rectangle bounds = Window.ClientBounds;
        return new Int2(Math.Max(bounds.Width, 1), Math.Max(bounds.Height, 1));
    }

    private void LoadUiShowcaseScene()
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        SceneSystem.SceneInstance.RootScene = content.Load<Scene>("UIShowcaseScene");
    }

    private static RuntimeMode ResolveRuntimeMode()
    {
        if (PendingLaunchTarget.HasValue)
        {
            RuntimeLaunchTarget target = PendingLaunchTarget.Value;
            PendingLaunchTarget = null;
            return target switch
            {
                RuntimeLaunchTarget.Prototype => RuntimeMode.Prototype,
                RuntimeLaunchTarget.UiShowcase => RuntimeMode.UiShowcase,
                _ => RuntimeMode.Terrain,
            };
        }

        return RuntimeModeResolver.Resolve();
    }

    private bool IsDevRouterSceneActive()
    {
        Scene? scene = SceneSystem.SceneInstance.RootScene;
        if (scene is null)
        {
            return false;
        }

        return scene.Entities.Any(entity => entity.Get<DevSceneRouterScript>() is not null);
    }
}
