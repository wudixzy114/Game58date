#nullable enable
using System;
using System.Linq;
using Game58date.Terrain;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;

namespace Game58date.Gameplay;

public static class RuntimeSceneLauncher
{
    private static RuntimeLaunchRequest? pendingLaunch;

    public static void Launch(IServiceRegistry services, SceneSystem sceneSystem, RuntimeLaunchTarget target)
    {
        Launch(services, sceneSystem, RuntimeLaunchRequest.Create(target));
    }

    public static void Launch(IServiceRegistry services, SceneSystem sceneSystem, RuntimeLaunchRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        pendingLaunch = request;
        sceneSystem.SceneInstance.RootScene = CreateLoadingScene();
    }

    public static RuntimeLaunchRequest? PeekPendingLaunch()
    {
        return pendingLaunch;
    }

    public static void CommitPendingLaunch(IServiceRegistry services, SceneSystem sceneSystem)
    {
        IContentManager? content = services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        RuntimeLaunchRequest request = pendingLaunch
            ?? throw new InvalidOperationException("No pending launch request was available.");
        pendingLaunch = null;
        TerrainRuntimeStartupOptions.SetPendingLaunchOverrides(request.TerrainOverrides);

        Scene scene = request.Target switch
        {
            RuntimeLaunchTarget.DevRouter => content.Load<Scene>("DevSceneRouter"),
            RuntimeLaunchTarget.UiShowcase => content.Load<Scene>("UIShowcaseScene"),
            RuntimeLaunchTarget.MainMenu => content.Load<Scene>("MainMenuScene"),
            _ => content.Load<Scene>("MainScene"),
        };

        sceneSystem.SceneInstance.RootScene = scene;

        switch (request.Target)
        {
            case RuntimeLaunchTarget.Terrain:
                EnsureRuntimeAnchor(scene, useLegacyPrototype: false);
                break;
            case RuntimeLaunchTarget.Prototype:
                EnsureRuntimeAnchor(scene, useLegacyPrototype: true);
                break;
        }
    }

    private static Scene CreateLoadingScene()
    {
        var scene = new Scene();

        var camera = new Entity("LoadingCamera");
        camera.Transform.Position = new Vector3(0f, 0f, 10f);
        camera.Add(new CameraComponent
        {
            NearClipPlane = 0.1f,
            FarClipPlane = 100f,
        });
        scene.Entities.Add(camera);

        var runtime = new Entity("LoadingRuntime");
        runtime.Add(new SceneLoadingRuntimeScript());
        scene.Entities.Add(runtime);

        return scene;
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
