#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class VoxelTerrainWorldRuntime
{
    private readonly TerrainGenerationSettings settings;
    private readonly VoxelChunkModelFactory modelFactory;
    private readonly VoxelChunkBuildPipeline buildPipeline;
    private readonly Dictionary<VoxelChunkCoordinate, VoxelChunkRuntime> chunks = new();
    private readonly HashSet<VoxelChunkCoordinate> desiredChunks = new();
    private int nextRevision = 1;

    public VoxelTerrainWorldRuntime(
        TerrainGenerationSettings settings,
        TerrainChunkGenerator generator,
        VoxelChunkMesher mesher,
        VoxelChunkModelFactory modelFactory)
    {
        this.settings = settings;
        this.modelFactory = modelFactory;
        buildPipeline = new VoxelChunkBuildPipeline(generator, mesher, settings.MaxConcurrentChunkBuilds);
        Stats = new TerrainRuntimeStats();
    }

    public TerrainRuntimeStats Stats { get; }

    public void RefreshVisibleChunks(Scene scene, Vector3 cameraPosition, bool force)
    {
        if (force)
        {
            Stats.VisibleFaceCount = 0;
        }

        desiredChunks.Clear();

        int chunkSizeWorld = settings.ChunkSize;
        int centerChunkX = (int)MathF.Floor(cameraPosition.X / chunkSizeWorld);
        int centerChunkZ = (int)MathF.Floor(cameraPosition.Z / chunkSizeWorld);

        for (int dz = -settings.ViewDistanceInChunks; dz <= settings.ViewDistanceInChunks; dz++)
        {
            for (int dx = -settings.ViewDistanceInChunks; dx <= settings.ViewDistanceInChunks; dx++)
            {
                var coordinate = new VoxelChunkCoordinate(centerChunkX + dx, centerChunkZ + dz);
                desiredChunks.Add(coordinate);

                if (force)
                {
                    RemoveActiveChunk(coordinate);
                    RequestChunkBuild(coordinate);
                }
                else if (!chunks.ContainsKey(coordinate) && !buildPipeline.HasOutstandingRequest(coordinate))
                {
                    RequestChunkBuild(coordinate);
                }
            }
        }

        var toRemove = new List<VoxelChunkCoordinate>();
        foreach ((VoxelChunkCoordinate coordinate, VoxelChunkRuntime runtime) in chunks)
        {
            if (!desiredChunks.Contains(coordinate))
            {
                runtime.Entity.Scene = null;
                Stats.VisibleFaceCount -= runtime.MeshData.FaceCount;
                toRemove.Add(coordinate);
            }
        }

        foreach (VoxelChunkCoordinate coordinate in toRemove)
        {
            chunks.Remove(coordinate);
        }

        buildPipeline.Pump();
        foreach (ChunkBuildResult result in buildPipeline.DequeueReadyResults(settings.MaxChunkUploadsPerFrame))
        {
            if (!desiredChunks.Contains(result.Coordinate))
            {
                continue;
            }

            IntegrateChunk(scene, result);
        }

        Stats.LoadedChunkCount = chunks.Count;
        Stats.QueuedBuildCount = buildPipeline.QueuedCount;
        Stats.RunningBuildCount = buildPipeline.RunningCount;
        Stats.LastErrorMessage = buildPipeline.LastErrorMessage;
    }

    private void RequestChunkBuild(VoxelChunkCoordinate coordinate)
    {
        buildPipeline.Enqueue(coordinate, nextRevision++);
    }

    private void RemoveActiveChunk(VoxelChunkCoordinate coordinate)
    {
        if (chunks.Remove(coordinate, out VoxelChunkRuntime? existing))
        {
            Stats.VisibleFaceCount -= existing.MeshData.FaceCount;
            existing.Entity.Scene = null;
        }
    }

    private void IntegrateChunk(Scene scene, ChunkBuildResult result)
    {
        RemoveActiveChunk(result.Coordinate);

        var entity = new Entity($"Chunk_{result.Coordinate.X}_{result.Coordinate.Z}");
        if (!result.MeshData.IsEmpty)
        {
            entity.Add(new ModelComponent(modelFactory.CreateModel(result.MeshData)));
        }

        scene.Entities.Add(entity);
        chunks[result.Coordinate] = new VoxelChunkRuntime(result.Coordinate, result.Data, result.MeshData, entity);
        Stats.VisibleFaceCount += result.MeshData.FaceCount;
    }
}
