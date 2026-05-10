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
    private readonly WindowsFullscreenController fullscreenController = new();
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
                Window.IsBorderLess = true;
                Window.FullscreenIsBorderlessWindow = true;
                Window.IsFullscreen = false;
                hasAppliedFullscreen = fullscreenController.TryApplyBorderlessFullscreen(Window, out Int2 nativeClientSize);
                if (hasAppliedFullscreen && nativeClientSize.X > 0 && nativeClientSize.Y > 0)
                {
                    lastWindowedSize = nativeClientSize;
                }
            }
        }
        else if (keepUnifiedFullscreen && Window.Visible && !Window.IsMinimized)
        {
            Window.IsBorderLess = true;
            Window.FullscreenIsBorderlessWindow = true;
            Window.IsFullscreen = false;

            if (!fullscreenController.IsCurrentlyBorderlessFullscreen(Window, out Int2 nativeClientSize))
            {
                if (fullscreenController.TryApplyBorderlessFullscreen(Window, out nativeClientSize) && nativeClientSize.X > 0 && nativeClientSize.Y > 0)
                {
                    lastWindowedSize = nativeClientSize;
                }
            }
            else if (nativeClientSize.X > 0 && nativeClientSize.Y > 0)
            {
                lastWindowedSize = nativeClientSize;
            }
        }

        SynchronizePresenterWithWindow();
        base.Update(gameTime);
    }

    private void ConfigureWindowForGameplay()
    {
        Window.AllowUserResizing = false;
        Window.IsBorderLess = true;
        Window.FullscreenIsBorderlessWindow = true;
        Window.IsFullscreen = false;

        Rectangle bounds = Window.ClientBounds;
        Int2 currentSize = new(Math.Max(bounds.Width, 1), Math.Max(bounds.Height, 1));
        lastWindowedSize = currentSize;
        Window.PreferredWindowedSize = currentSize;
        Window.PreferredFullscreenSize = currentSize;

        IsMouseVisible = false;
    }

    private void SynchronizePresenterWithWindow()
    {
        if (Window.IsMinimized || !Window.Visible || GraphicsDevice?.Presenter?.BackBuffer is null)
        {
            return;
        }

        Int2 targetSize;
        if (keepUnifiedFullscreen && hasAppliedFullscreen && fullscreenController.TryGetClientSize(out Int2 nativeClientSize))
        {
            targetSize = nativeClientSize;
        }
        else
        {
            targetSize = GetCurrentClientSize();
        }

        lastWindowedSize = targetSize;

        var presenter = GraphicsDevice.Presenter;
        var backBuffer = presenter.BackBuffer;
        if (backBuffer.Width == targetSize.X && backBuffer.Height == targetSize.Y)
        {
            return;
        }

        presenter.Resize(targetSize.X, targetSize.Y, presenter.Description.BackBufferFormat);
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
