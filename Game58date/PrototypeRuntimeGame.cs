#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Game58date.Gameplay;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Game58date.Terrain;

namespace Game58date;

public sealed class PrototypeRuntimeGame : Game
{
    private TimeSpan fullscreenAttemptDelay = TimeSpan.FromMilliseconds(150);
    private bool hasAppliedFullscreen;

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        ConfigureWindowForGameplay();
        RuntimeMode runtimeMode = RuntimeModeResolver.Resolve();

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
                Window.IsBorderLess = true;
                Window.FullscreenIsBorderlessWindow = true;
                Window.IsFullscreen = true;
                hasAppliedFullscreen = true;
            }
        }

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
        Window.PreferredWindowedSize = currentSize;
        Window.PreferredFullscreenSize = currentSize;

        IsMouseVisible = false;
    }
}
