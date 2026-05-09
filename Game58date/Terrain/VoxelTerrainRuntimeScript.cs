#nullable enable
using System;
using Stride.Core.Mathematics;
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
    private readonly VoxelChunkOverrideStore overrideStore = new();

    private VoxelTerrainWorldRuntime? worldRuntime;
    private Entity? cameraEntity;
    private Entity? playerEntity;
    private BasicCameraController? observerCameraController;
    private FirstPersonCharacterController? firstPersonController;
    private Scene? scene;
    private TerrainViewMode viewMode = TerrainViewMode.Observer;
    private Vector3 observerResetPosition;
    private Quaternion observerResetRotation;
    private Vector3 playerResetPosition;

    public override void Start()
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        scene = Entity.Scene ?? throw new InvalidOperationException("Terrain runtime requires an active scene.");

        var generator = new TerrainChunkGenerator(settings, overrideStore);
        var mesher = new VoxelChunkMesher(settings);
        var modelFactory = new VoxelChunkModelFactory(Game.GraphicsDevice, content);
        worldRuntime = new VoxelTerrainWorldRuntime(settings, overrideStore, generator, mesher, modelFactory);

        cameraEntity = sceneBootstrapper.EnsureCamera(scene);
        observerCameraController = cameraEntity.Get<BasicCameraController>();
        sceneBootstrapper.EnsureTerrainLighting(scene);
        sceneBootstrapper.DisableLegacyEntities(scene);
        worldRuntime.WarmupSpawnArea(scene, cameraEntity.Transform.Position, radiusInChunks: 1);
        worldRuntime.RefreshVisibleChunks(scene, cameraEntity.Transform.Position, force: false);

        Vector3 spawnPosition = GetPlayerSpawnPosition(cameraEntity.Transform.Position);
        playerEntity = sceneBootstrapper.EnsureFirstPersonPlayer(scene, spawnPosition, cameraEntity);
        firstPersonController = playerEntity.Get<FirstPersonCharacterController>();
        firstPersonController?.SnapTo(spawnPosition);

        observerResetPosition = cameraEntity.Transform.Position;
        observerResetRotation = cameraEntity.Transform.Rotation;
        playerResetPosition = spawnPosition;
        SetViewMode(TerrainViewMode.FirstPerson);
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

        if (Input.IsKeyPressed(Keys.F5))
        {
            SetViewMode(viewMode == TerrainViewMode.FirstPerson ? TerrainViewMode.Observer : TerrainViewMode.FirstPerson);
        }

        if (Input.IsKeyPressed(Keys.F6))
        {
            ResetCurrentModePosition();
        }

        if (Input.IsKeyPressed(Keys.F8))
        {
            worldRuntime.SetBlockWorld(0, settings.WaterLevel + 1, 0, BlockKind.Air);
        }

        worldRuntime.RefreshVisibleChunks(scene, cameraEntity.Transform.Position, force);
        DebugTextSystem? debugText = (Game as Game)?.DebugTextSystem;
        debugOverlay.Draw(debugText, worldRuntime.Stats, settings, viewMode);
    }

    private void SetViewMode(TerrainViewMode nextMode)
    {
        if (cameraEntity is null || firstPersonController is null)
        {
            return;
        }

        if (viewMode == nextMode)
        {
            return;
        }

        if (nextMode == TerrainViewMode.FirstPerson)
        {
            if (observerCameraController is not null)
            {
                observerCameraController.IsControlEnabled = false;
            }

            Vector3 safePosition = GetPlayerSpawnPosition(cameraEntity.Transform.Position);
            firstPersonController.MatchCameraPose(safePosition + Vector3.UnitY * TerrainSceneBootstrapper.PlayerEyeHeightFromCenter, cameraEntity.Transform.Rotation);
            firstPersonController.SetActiveMode(true);
        }
        else
        {
            firstPersonController.SetActiveMode(false);
            if (observerCameraController is not null)
            {
                observerCameraController.IsControlEnabled = true;
            }

            Game.IsMouseVisible = true;
        }

        viewMode = nextMode;
    }

    private void ResetCurrentModePosition()
    {
        if (cameraEntity is null || firstPersonController is null)
        {
            return;
        }

        if (viewMode == TerrainViewMode.FirstPerson)
        {
            firstPersonController.SnapTo(playerResetPosition);
            firstPersonController.SetYawPitch(0f, 0f);
            TerrainRuntimeLogger.Logger.Info("Reset first-person player to spawn position.");
            return;
        }

        cameraEntity.Transform.Position = observerResetPosition;
        cameraEntity.Transform.Rotation = observerResetRotation;
        TerrainRuntimeLogger.Logger.Info("Reset observer camera to default debug position.");
    }

    private Vector3 GetPlayerSpawnPosition(Vector3 fallbackPosition)
    {
        if (worldRuntime is null)
        {
            return fallbackPosition;
        }

        return worldRuntime.ResolveSafeSpawnPosition(fallbackPosition, searchRadius: 3);
    }
}
