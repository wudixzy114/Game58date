#nullable enable
using System;
using System.Linq;
using Game58date.Terrain;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Engine;

namespace Game58date.Gameplay;

public static class RuntimeSceneLauncher
{
    public static void Launch(IServiceRegistry services, SceneSystem sceneSystem, RuntimeLaunchTarget target)
    {
        IContentManager? content = services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        Scene scene = target switch
        {
            RuntimeLaunchTarget.DevRouter => content.Load<Scene>("DevSceneRouter"),
            RuntimeLaunchTarget.UiShowcase => content.Load<Scene>("UIShowcaseScene"),
            RuntimeLaunchTarget.MainMenu => content.Load<Scene>("MainMenuScene"),
            _ => content.Load<Scene>("MainScene"),
        };

        sceneSystem.SceneInstance.RootScene = scene;

        switch (target)
        {
            case RuntimeLaunchTarget.Terrain:
                EnsureRuntimeAnchor(scene, useLegacyPrototype: false);
                break;
            case RuntimeLaunchTarget.Prototype:
                EnsureRuntimeAnchor(scene, useLegacyPrototype: true);
                break;
        }
    }

    private static void EnsureRuntimeAnchor(Scene scene, bool useLegacyPrototype)
    {
        string anchorName = useLegacyPrototype ? "LegacyPrototypeRuntime" : "PrototypeRuntime";
        Entity? anchor = scene.Entities.FirstOrDefault(entity => entity.Name == anchorName);
        if (anchor is not null)
        {
            return;
        }

        anchor = new Entity(anchorName);
        if (useLegacyPrototype)
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
