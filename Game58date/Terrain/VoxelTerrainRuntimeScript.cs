#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;

namespace Game58date.Terrain;

public sealed class VoxelTerrainRuntimeScript : SyncScript
{
    private readonly TerrainGenerationSettings settings = new();
    private readonly Dictionary<VoxelChunkCoordinate, VoxelChunkRuntime> chunks = new();

    private TerrainChunkGenerator? generator;
    private VoxelChunkMesher? mesher;
    private VoxelChunkModelFactory? modelFactory;
    private Entity? cameraEntity;
    private int loadedChunkCount;
    private int visibleFaceCount;
    private bool wireframeHintShown;

    public override void Start()
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        generator = new TerrainChunkGenerator(settings);
        mesher = new VoxelChunkMesher(settings);
        modelFactory = new VoxelChunkModelFactory(Game.GraphicsDevice, content);

        CreateCameraIfNeeded();
        DisableLegacyEntities();
        RebuildVisibleTerrain(force: true);
    }

    public override void Update()
    {
        if (Input.IsKeyPressed(Keys.F7))
        {
            RebuildVisibleTerrain(force: true);
        }

        RebuildVisibleTerrain(force: false);
        DrawDebugOverlay();
    }

    private void CreateCameraIfNeeded()
    {
        cameraEntity = FindEntity("Camera");
        if (cameraEntity is not null)
        {
            return;
        }

        cameraEntity = new Entity("RuntimeCamera");
        cameraEntity.Transform.Position = new Stride.Core.Mathematics.Vector3(24f, 34f, -24f);
        cameraEntity.Transform.RotationEulerXYZ = new Stride.Core.Mathematics.Vector3(0.55f, 0.75f, 0f);
        cameraEntity.Add(new CameraComponent());
        cameraEntity.Add(new BasicCameraController
        {
            KeyboardMovementSpeed = new Stride.Core.Mathematics.Vector3(18f, 18f, 18f),
            SpeedFactor = 4f,
        });
        Entity.Scene?.Entities.Add(cameraEntity);
    }

    private void DisableLegacyEntities()
    {
        foreach (string name in new[] { "Ground", "Sphere" })
        {
            Entity? legacy = FindEntity(name);
            if (legacy is not null)
            {
                ModelComponent? model = legacy.Get<ModelComponent>();
                if (model is not null)
                {
                    model.Enabled = false;
                }
            }
        }
    }

    private void RebuildVisibleTerrain(bool force)
    {
        if (cameraEntity is null || generator is null || mesher is null || modelFactory is null || Entity.Scene is null)
        {
            return;
        }

        if (force)
        {
            visibleFaceCount = 0;
        }

        var cameraPosition = cameraEntity.Transform.Position;
        int chunkSizeWorld = settings.ChunkSize;
        int centerChunkX = (int)MathF.Floor(cameraPosition.X / chunkSizeWorld);
        int centerChunkZ = (int)MathF.Floor(cameraPosition.Z / chunkSizeWorld);

        var required = new HashSet<VoxelChunkCoordinate>();
        for (int dz = -settings.ViewDistanceInChunks; dz <= settings.ViewDistanceInChunks; dz++)
        {
            for (int dx = -settings.ViewDistanceInChunks; dx <= settings.ViewDistanceInChunks; dx++)
            {
                var coordinate = new VoxelChunkCoordinate(centerChunkX + dx, centerChunkZ + dz);
                required.Add(coordinate);

                if (force || !chunks.ContainsKey(coordinate))
                {
                    if (force && chunks.Remove(coordinate, out VoxelChunkRuntime? existing))
                    {
                        visibleFaceCount -= existing.MeshData.FaceCount;
                        existing.Entity.Scene = null;
                    }

                    SpawnChunk(coordinate, generator, mesher, modelFactory);
                }
            }
        }

        var toRemove = new List<VoxelChunkCoordinate>();
        foreach ((VoxelChunkCoordinate coordinate, VoxelChunkRuntime runtime) in chunks)
        {
            if (!required.Contains(coordinate))
            {
                runtime.Entity.Scene = null;
                visibleFaceCount -= runtime.MeshData.FaceCount;
                toRemove.Add(coordinate);
            }
        }

        foreach (VoxelChunkCoordinate coordinate in toRemove)
        {
            chunks.Remove(coordinate);
        }

        loadedChunkCount = chunks.Count;
    }

    private void SpawnChunk(VoxelChunkCoordinate coordinate, TerrainChunkGenerator generator, VoxelChunkMesher mesher, VoxelChunkModelFactory modelFactory)
    {
        VoxelChunkData data = generator.Generate(coordinate);
        VoxelChunkMeshData meshData = mesher.Build(data);
        visibleFaceCount += meshData.FaceCount;

        var entity = new Entity($"Chunk_{coordinate.X}_{coordinate.Z}");
        if (!meshData.IsEmpty)
        {
            Model model = modelFactory.CreateModel(meshData);
            entity.Add(new ModelComponent(model));
        }

        Entity.Scene!.Entities.Add(entity);
        chunks[coordinate] = new VoxelChunkRuntime(coordinate, data, meshData, entity);
    }

    private void DrawDebugOverlay()
    {
        DebugTextSystem? debugText = (Game as Game)?.DebugTextSystem;
        if (debugText is null)
        {
            return;
        }

        if (!wireframeHintShown)
        {
            wireframeHintShown = true;
        }

        debugText.Print("Voxel terrain runtime", new Stride.Core.Mathematics.Int2(20, 20), new Stride.Core.Mathematics.Color4(1f, 0.95f, 0.65f, 1f));
        debugText.Print($"Chunks loaded: {loadedChunkCount}", new Stride.Core.Mathematics.Int2(20, 44), new Stride.Core.Mathematics.Color4(1f));
        debugText.Print($"Faces built: {visibleFaceCount}", new Stride.Core.Mathematics.Int2(20, 68), new Stride.Core.Mathematics.Color4(1f));
        debugText.Print($"Chunk size: {settings.ChunkSize}x{settings.ChunkHeight}x{settings.ChunkSize}", new Stride.Core.Mathematics.Int2(20, 92), new Stride.Core.Mathematics.Color4(1f));
        debugText.Print("F7 rebuilds visible terrain", new Stride.Core.Mathematics.Int2(20, 116), new Stride.Core.Mathematics.Color4(0.75f, 0.75f, 0.75f, 1f));
    }

    private Entity? FindEntity(string name)
    {
        if (Entity.Scene is null)
        {
            return null;
        }

        foreach (Entity entity in Entity.Scene.Entities)
        {
            if (entity.Name == name)
            {
                return entity;
            }
        }

        return null;
    }
}
