#nullable enable
using System;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Input;
using Stride.Profiling;

namespace Game58date.Terrain;

public sealed class VoxelTerrainRuntimeScript : SyncScript
{
    private readonly TerrainGenerationSettings settings = new();
    private readonly TerrainSceneBootstrapper sceneBootstrapper = new();
    private readonly TerrainDebugOverlay debugOverlay = new();

    private VoxelTerrainWorldRuntime? worldRuntime;
    private Entity? cameraEntity;
    private Scene? scene;

    public override void Start()
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        scene = Entity.Scene ?? throw new InvalidOperationException("Terrain runtime requires an active scene.");

        var generator = new TerrainChunkGenerator(settings);
        var mesher = new VoxelChunkMesher(settings);
        var modelFactory = new VoxelChunkModelFactory(Game.GraphicsDevice, content);
        worldRuntime = new VoxelTerrainWorldRuntime(settings, generator, mesher, modelFactory);

        cameraEntity = sceneBootstrapper.EnsureCamera(scene);
        sceneBootstrapper.EnsureTerrainLighting(scene);
        sceneBootstrapper.DisableLegacyEntities(scene);
        worldRuntime.RefreshVisibleChunks(scene, cameraEntity.Transform.Position, force: true);
    }

    public override void Update()
    {
        if (scene is null || cameraEntity is null || worldRuntime is null)
        {
            return;
        }

        bool force = false;
        if (Input.IsKeyPressed(Keys.F7))
        {
            force = true;
        }

        worldRuntime.RefreshVisibleChunks(scene, cameraEntity.Transform.Position, force);
        DebugTextSystem? debugText = (Game as Game)?.DebugTextSystem;
        debugOverlay.Draw(debugText, worldRuntime.Stats, settings);
    }
}
