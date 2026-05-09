#nullable enable
using System;
using Game58date.Gameplay;
using Game58date.Save;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Physics;

namespace Game58date.Terrain;

public sealed class VoxelTerrainRuntimeScript : SyncScript
{
    private readonly TerrainSceneBootstrapper sceneBootstrapper = new();
    private readonly VoxelChunkOverrideStore overrideStore = new();
    private readonly GameSaveRepository saveRepository = new();

    private TerrainGenerationSettings settings = new();
    private VoxelTerrainWorldRuntime? worldRuntime;
    private Entity? cameraEntity;
    private FirstPersonCharacterController? firstPersonController;
    private Scene? scene;
    private Simulation? simulation;
    private Quaternion spawnRotation = Quaternion.Identity;
    private GameSaveData? activeSaveData;
    private WorldLawRuntimeController? worldLawController;
    private TerrainRuntimeStartupOptions startupOptions = new();
    private float autosaveCountdown;
    private int lastSavedOverrideRevision = -1;
    private Vector3 lastSavedEyePosition;
    private Quaternion lastSavedRotation = Quaternion.Identity;
    private float lastSavedWorldLawTime;

    public override void Start()
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        scene = Entity.Scene ?? throw new InvalidOperationException("Terrain runtime requires an active scene.");
        simulation = Services.GetService<Simulation>();

        startupOptions = TerrainRuntimeStartupOptions.FromEnvironment(settings);
        activeSaveData = saveRepository.LoadOrCreate(startupOptions.SaveSlotName, startupOptions.PreferredSeed);
        settings = settings.WithSeed(activeSaveData.World.Seed ?? startupOptions.PreferredSeed);
        overrideStore.ReplaceAll(VoxelChunkOverrideSaveMapper.BuildOverrideSnapshot(activeSaveData.Terrain, settings));
        autosaveCountdown = startupOptions.AutosaveIntervalSeconds;

        var generator = new TerrainChunkGenerator(settings, overrideStore);
        var mesher = new VoxelChunkMesher(settings);
        var modelFactory = new VoxelChunkModelFactory(Game.GraphicsDevice, Game.GraphicsContext, content);
        worldRuntime = new VoxelTerrainWorldRuntime(settings, overrideStore, generator, mesher, modelFactory);

        cameraEntity = sceneBootstrapper.EnsureCamera(scene);
        firstPersonController = sceneBootstrapper.EnsureFirstPersonController(cameraEntity, worldRuntime);
        Entity directionalLightEntity = sceneBootstrapper.EnsureTerrainLighting(scene);
        sceneBootstrapper.PruneLegacySceneEntities(scene);
        worldLawController = EnsureWorldLawController(cameraEntity, directionalLightEntity, activeSaveData.Gameplay);

        Vector3 desiredSpawnPosition = cameraEntity.Transform.Position;
        if (activeSaveData.Player.EyePosition is { } savedEyePosition && savedEyePosition.IsFinite)
        {
            desiredSpawnPosition = savedEyePosition.ToStrideVector3();
        }

        spawnRotation = cameraEntity.Transform.Rotation;
        if (activeSaveData.Player.Rotation is { } savedRotation && savedRotation.IsFiniteAndNonZero)
        {
            spawnRotation = Quaternion.Normalize(savedRotation.ToStrideQuaternion());
        }

        worldRuntime.WarmupSpawnArea(scene, desiredSpawnPosition, radiusInChunks: 1);
        Vector3 spawnEyePosition = ResolveSpawnEyePosition(desiredSpawnPosition, "startup");
        worldRuntime.WarmupSpawnArea(scene, spawnEyePosition, radiusInChunks: 1);
        worldRuntime.RefreshVisibleChunks(scene, spawnEyePosition, force: false);

        firstPersonController.MatchPose(spawnEyePosition, spawnRotation);
        firstPersonController.SetActiveMode(true);
        CacheLastSavedPose(spawnEyePosition, spawnRotation);
        SaveRuntimeState(force: true, reason: "startup-sync");
    }

    public override void Update()
    {
        if (scene is null || cameraEntity is null || worldRuntime is null || firstPersonController is null)
        {
            return;
        }

        worldRuntime.RefreshVisibleChunks(scene, firstPersonController.EyePosition, force: false);
        UpdateAutosave(firstPersonController);
    }

    public override void Cancel()
    {
        SaveRuntimeState(force: true, reason: "shutdown");
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

    private void UpdateAutosave(FirstPersonCharacterController controller)
    {
        if (!startupOptions.AutosaveEnabled)
        {
            return;
        }

        float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        autosaveCountdown -= deltaTime;
        if (autosaveCountdown > 0f)
        {
            return;
        }

        autosaveCountdown = startupOptions.AutosaveIntervalSeconds;
        SaveRuntimeState(force: false, reason: "autosave", controller);
    }

    private void SaveRuntimeState(bool force, string reason, FirstPersonCharacterController? controllerOverride = null)
    {
        if (activeSaveData is null || worldRuntime is null || firstPersonController is null && controllerOverride is null)
        {
            return;
        }

        FirstPersonCharacterController controller = controllerOverride ?? firstPersonController!;
        Vector3 currentEyePosition = controller.EyePosition;
        Quaternion currentRotation = controller.Entity.Transform.Rotation;
        int currentOverrideRevision = overrideStore.Revision;
        float currentWorldLawTime = worldLawController?.RuntimeState.WorldTimeSeconds ?? 0f;

        bool poseChanged = !NearlyEqual(lastSavedEyePosition, currentEyePosition) || !NearlyEqual(lastSavedRotation, currentRotation);
        bool overridesChanged = lastSavedOverrideRevision != currentOverrideRevision;
        bool worldLawChanged = !MathUtil.NearEqual(lastSavedWorldLawTime, currentWorldLawTime);
        if (!force && !poseChanged && !overridesChanged && !worldLawChanged)
        {
            return;
        }

        activeSaveData.World.Seed = settings.Seed;
        activeSaveData.Player.EyePosition = SerializableVector3.FromStride(currentEyePosition);
        activeSaveData.Player.Rotation = SerializableQuaternion.FromStride(currentRotation);
        activeSaveData.Terrain = VoxelChunkOverrideSaveMapper.CreateTerrainSaveData(settings, overrideStore);
        activeSaveData.Gameplay = WorldLawSaveMapper.CreateSaveData(worldLawController?.CreateSnapshot() ?? new WorldLawRuntimeState());

        if (!saveRepository.Save(activeSaveData))
        {
            return;
        }

        CacheLastSavedPose(currentEyePosition, currentRotation);
        lastSavedOverrideRevision = currentOverrideRevision;
        lastSavedWorldLawTime = currentWorldLawTime;
        TerrainRuntimeLogger.Logger.Info(
            $"Saved runtime state reason={reason} slot={activeSaveData.SlotName} seed={settings.Seed} overrides={overrideStore.GetTotalOverrideCount()} eye=({currentEyePosition.X:F2},{currentEyePosition.Y:F2},{currentEyePosition.Z:F2}).");
    }

    private void CacheLastSavedPose(Vector3 eyePosition, Quaternion rotation)
    {
        lastSavedEyePosition = eyePosition;
        lastSavedRotation = Quaternion.Normalize(rotation);
    }

    private WorldLawRuntimeController EnsureWorldLawController(Entity camera, Entity directionalLightEntity, GameplaySaveData gameplaySaveData)
    {
        WorldLawRuntimeController? controller = Entity.Get<WorldLawRuntimeController>();
        if (controller is null)
        {
            controller = new WorldLawRuntimeController();
            controller.Initialize(WorldLawSaveMapper.BuildRuntimeState(gameplaySaveData), camera, directionalLightEntity);
            Entity.Add(controller);
            return controller;
        }

        controller.Initialize(WorldLawSaveMapper.BuildRuntimeState(gameplaySaveData), camera, directionalLightEntity);
        return controller;
    }

    private static bool NearlyEqual(Vector3 left, Vector3 right)
    {
        return Vector3.DistanceSquared(left, right) <= 0.0004f;
    }

    private static bool NearlyEqual(Quaternion left, Quaternion right)
    {
        Quaternion normalizedLeft = Quaternion.Normalize(left);
        Quaternion normalizedRight = Quaternion.Normalize(right);
        float dot = MathF.Abs(Quaternion.Dot(normalizedLeft, normalizedRight));
        return dot >= 0.9999f;
    }
}
