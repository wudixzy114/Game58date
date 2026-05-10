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
    private readonly bool keepUnifiedFullscreen = true;
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
        if (keepUnifiedFullscreen && !hasAppliedFullscreen)
        {
            fullscreenAttemptDelay -= gameTime.Elapsed;
            if (fullscreenAttemptDelay <= TimeSpan.Zero && Window.Visible && !Window.IsMinimized)
            {
                EnsureUnifiedFullscreenWindowed();
                hasAppliedFullscreen = true;
            }
        }
        else if (keepUnifiedFullscreen && Window.Visible && !Window.IsMinimized)
        {
            EnsureUnifiedFullscreenWindowed();
        }

        SynchronizePresenterWithWindow();
        base.Update(gameTime);
    }

    private void ConfigureWindowForGameplay()
    {
        Window.AllowUserResizing = false;
        Window.IsBorderLess = keepUnifiedFullscreen;
        Window.FullscreenIsBorderlessWindow = keepUnifiedFullscreen;
        Window.IsFullscreen = false;

        Rectangle bounds = Window.ClientBounds;
        Int2 currentSize = new(Math.Max(bounds.Width, 1), Math.Max(bounds.Height, 1));
        lastWindowedSize = currentSize;
        Window.PreferredWindowedSize = keepUnifiedFullscreen
            ? GetPreferredFullscreenSize()
            : currentSize;
        Window.PreferredFullscreenSize = GetPreferredFullscreenSize();

        IsMouseVisible = false;
    }

    private void EnsureUnifiedFullscreenWindowed()
    {
        Int2 targetSize = GetPreferredFullscreenSize();
        if (targetSize.X <= 0 || targetSize.Y <= 0)
        {
            return;
        }

        if (Window.IsFullscreen)
        {
            Window.IsFullscreen = false;
        }

        Window.PreferredWindowedSize = targetSize;
        Window.FullscreenIsBorderlessWindow = true;
        Window.IsBorderLess = true;

        if (TryGetTargetOutputBounds(out Rectangle bounds))
        {
            Window.Position = new Int2(bounds.X, bounds.Y);
        }
    }

    private void SynchronizePresenterWithWindow()
    {
        if (Window.IsMinimized || !Window.Visible || GraphicsDevice?.Presenter?.BackBuffer is null)
        {
            return;
        }

        bool useUnifiedFullscreenWindow = keepUnifiedFullscreen && hasAppliedFullscreen;
        Int2 targetSize = useUnifiedFullscreenWindow
            ? GetPreferredFullscreenSize()
            : GetCurrentClientSize();

        if (useUnifiedFullscreenWindow)
        {
            if (Window.PreferredWindowedSize.X != targetSize.X || Window.PreferredWindowedSize.Y != targetSize.Y)
            {
                Window.PreferredWindowedSize = targetSize;
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

    private bool TryGetTargetOutputBounds(out Rectangle bounds)
    {
        bounds = default;
        if (GraphicsDevice?.Adapter is null)
        {
            return false;
        }

        Int2 clientSize = GetCurrentClientSize();
        Int2 windowPosition = Window.Position;
        int centerX = windowPosition.X + clientSize.X / 2;
        int centerY = windowPosition.Y + clientSize.Y / 2;

        GraphicsOutput? selectedOutput = null;
        foreach (var output in GraphicsDevice.Adapter.Outputs)
        {
            selectedOutput ??= output;

            Rectangle candidateBounds = output.DesktopBounds;
            if (centerX >= candidateBounds.X &&
                centerX < candidateBounds.X + candidateBounds.Width &&
                centerY >= candidateBounds.Y &&
                centerY < candidateBounds.Y + candidateBounds.Height)
            {
                selectedOutput = output;
                break;
            }
        }

        if (selectedOutput is null)
        {
            return false;
        }

        bounds = selectedOutput.DesktopBounds;
        return true;
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
