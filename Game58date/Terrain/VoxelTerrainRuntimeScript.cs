#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Physics;

namespace Game58date.Terrain;

public sealed class VoxelTerrainRuntimeScript : SyncScript
{
    private readonly TerrainGenerationSettings settings = new();
    private readonly TerrainSceneBootstrapper sceneBootstrapper = new();
    private readonly VoxelChunkOverrideStore overrideStore = new();

    private VoxelTerrainWorldRuntime? worldRuntime;
    private Entity? cameraEntity;
    private FirstPersonCharacterController? firstPersonController;
    private Scene? scene;
    private Simulation? simulation;
    private Quaternion spawnRotation = Quaternion.Identity;

    public override void Start()
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        scene = Entity.Scene ?? throw new InvalidOperationException("Terrain runtime requires an active scene.");
        simulation = Services.GetService<Simulation>();

        var generator = new TerrainChunkGenerator(settings, overrideStore);
        var mesher = new VoxelChunkMesher(settings);
        var modelFactory = new VoxelChunkModelFactory(Game.GraphicsDevice, content);
        worldRuntime = new VoxelTerrainWorldRuntime(settings, overrideStore, generator, mesher, modelFactory);

        cameraEntity = sceneBootstrapper.EnsureCamera(scene);
        firstPersonController = sceneBootstrapper.EnsureFirstPersonController(cameraEntity, worldRuntime);
        sceneBootstrapper.EnsureTerrainLighting(scene);
        sceneBootstrapper.PruneLegacySceneEntities(scene);

        Vector3 desiredSpawnPosition = cameraEntity.Transform.Position;
        spawnRotation = cameraEntity.Transform.Rotation;

        worldRuntime.WarmupSpawnArea(scene, desiredSpawnPosition, radiusInChunks: 1);
        Vector3 spawnEyePosition = ResolveSpawnEyePosition(desiredSpawnPosition, "startup");
        worldRuntime.WarmupSpawnArea(scene, spawnEyePosition, radiusInChunks: 1);
        worldRuntime.RefreshVisibleChunks(scene, spawnEyePosition, force: false);

        firstPersonController.MatchPose(spawnEyePosition, spawnRotation);
        firstPersonController.SetActiveMode(true);
    }

    public override void Update()
    {
        if (scene is null || cameraEntity is null || worldRuntime is null || firstPersonController is null)
        {
            return;
        }

        worldRuntime.RefreshVisibleChunks(scene, firstPersonController.EyePosition, force: false);
    }

    public override void Cancel()
    {
        firstPersonController?.SetActiveMode(false);
        base.Cancel();
    }

    private Vector3 ResolveSpawnEyePosition(Vector3 desiredEyePosition, string context)
    {
        if (worldRuntime is null)
        {
            return desiredEyePosition;
        }

        Vector3 candidate = worldRuntime.ResolveSafeSpawnPosition(desiredEyePosition, searchRadius: 3);
        Vector3 final = candidate;
        string rayInfo = "physics=n/a";

        simulation ??= Services.GetService<Simulation>();
        if (simulation is not null)
        {
            Vector3 rayFrom = new(candidate.X, settings.ChunkHeight + 24f, candidate.Z);
            Vector3 rayTo = new(candidate.X, -16f, candidate.Z);
            if (simulation.Raycast(rayFrom, rayTo, out HitResult hit, CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags.AllFilter, false, 0))
            {
                final = new Vector3(candidate.X, hit.Point.Y + TerrainSceneBootstrapper.PlayerEyeHeightFromFeet + 0.15f, candidate.Z);
                rayInfo = $"physics=hit y={hit.Point.Y:F2} collider={hit.Collider?.Entity?.Name ?? "unknown"}";
            }
            else
            {
                rayInfo = "physics=miss";
            }
        }

        string message = $"Spawn[{context}] desired=({desiredEyePosition.X:F2},{desiredEyePosition.Y:F2},{desiredEyePosition.Z:F2}) candidate=({candidate.X:F2},{candidate.Y:F2},{candidate.Z:F2}) final=({final.X:F2},{final.Y:F2},{final.Z:F2}) {rayInfo}";
        TerrainRuntimeLogger.Logger.Info(message);
        return final;
    }
}
