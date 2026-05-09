#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class VoxelTerrainWorldRuntime
{
    private readonly TerrainGenerationSettings settings;
    private readonly VoxelChunkOverrideStore overrideStore;
    private readonly TerrainChunkGenerator generator;
    private readonly VoxelChunkMesher mesher;
    private readonly VoxelChunkCollisionBuilder collisionBuilder;
    private readonly VoxelChunkModelFactory modelFactory;
    private readonly VoxelChunkBuildPipeline buildPipeline;
    private readonly Dictionary<VoxelChunkCoordinate, VoxelChunkRuntime> chunks = new();
    private readonly HashSet<VoxelChunkCoordinate> desiredChunks = new();
    private int nextRevision = 1;

    public VoxelTerrainWorldRuntime(
        TerrainGenerationSettings settings,
        VoxelChunkOverrideStore overrideStore,
        TerrainChunkGenerator generator,
        VoxelChunkMesher mesher,
        VoxelChunkModelFactory modelFactory)
    {
        this.settings = settings;
        this.overrideStore = overrideStore;
        this.generator = generator;
        this.mesher = mesher;
        collisionBuilder = new VoxelChunkCollisionBuilder(settings);
        this.modelFactory = modelFactory;
        buildPipeline = new VoxelChunkBuildPipeline(generator, mesher, collisionBuilder, settings.MaxConcurrentChunkBuilds);
        Stats = new TerrainRuntimeStats();
    }

    public TerrainRuntimeStats Stats { get; }

    public void WarmupSpawnArea(Scene scene, Vector3 worldPosition, int radiusInChunks)
    {
        int chunkSizeWorld = settings.ChunkSize;
        int centerChunkX = (int)MathF.Floor(worldPosition.X / chunkSizeWorld);
        int centerChunkZ = (int)MathF.Floor(worldPosition.Z / chunkSizeWorld);

        for (int dz = -radiusInChunks; dz <= radiusInChunks; dz++)
        {
            for (int dx = -radiusInChunks; dx <= radiusInChunks; dx++)
            {
                var coordinate = new VoxelChunkCoordinate(centerChunkX + dx, centerChunkZ + dz);
                if (chunks.ContainsKey(coordinate))
                {
                    continue;
                }

                ChunkBuildResult result = BuildChunkImmediate(coordinate);
                IntegrateChunk(scene, result);
            }
        }
    }

    public bool TryGetSurfaceHeightWorld(int worldX, int worldZ, out int surfaceHeight)
    {
        int chunkX = VoxelGridMath.FloorDiv(worldX, settings.ChunkSize);
        int chunkZ = VoxelGridMath.FloorDiv(worldZ, settings.ChunkSize);
        int localX = VoxelGridMath.PositiveMod(worldX, settings.ChunkSize);
        int localZ = VoxelGridMath.PositiveMod(worldZ, settings.ChunkSize);
        var coordinate = new VoxelChunkCoordinate(chunkX, chunkZ);

        if (chunks.TryGetValue(coordinate, out VoxelChunkRuntime? runtime))
        {
            surfaceHeight = runtime.Data.GetSurfaceHeight(localX, localZ);
            return true;
        }

        VoxelChunkData sampledChunk = generator.Generate(coordinate);
        surfaceHeight = sampledChunk.GetSurfaceHeight(localX, localZ);
        return true;
    }

    public Vector3 ResolveSafeSpawnPosition(Vector3 desiredPosition, int searchRadius)
    {
        int baseX = (int)MathF.Floor(desiredPosition.X);
        int baseZ = (int)MathF.Floor(desiredPosition.Z);
        float bestScore = float.MaxValue;
        Vector3 bestPosition = desiredPosition;
        bool found = false;
        int highestSurface = int.MinValue;
        int highestSurfaceX = baseX;
        int highestSurfaceZ = baseZ;

        for (int dz = -searchRadius; dz <= searchRadius; dz++)
        {
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                int worldX = baseX + dx;
                int worldZ = baseZ + dz;
                if (!TryGetSurfaceHeightWorld(worldX, worldZ, out int surfaceHeight))
                {
                    continue;
                }

                if (surfaceHeight > highestSurface)
                {
                    highestSurface = surfaceHeight;
                    highestSurfaceX = worldX;
                    highestSurfaceZ = worldZ;
                }

                if (!HasStandingClearance(worldX, surfaceHeight, worldZ))
                {
                    continue;
                }

                float horizontalDistance = dx * dx + dz * dz;
                float spawnY = ComputeSpawnY(surfaceHeight);
                float verticalPenalty = MathF.Abs(spawnY - desiredPosition.Y) * 0.15f;
                float score = horizontalDistance + verticalPenalty;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestPosition = new Vector3(worldX + 0.5f, spawnY, worldZ + 0.5f);
                found = true;
            }
        }

        if (found)
        {
            return bestPosition;
        }

        if (highestSurface != int.MinValue)
        {
            TerrainRuntimeLogger.Logger.Warning(
                $"No clearance for spawn near ({baseX},{baseZ}); falling back to top of highest column at ({highestSurfaceX},{highestSurfaceZ}) y={highestSurface}.");
            return new Vector3(
                highestSurfaceX + 0.5f,
                ComputeSpawnY(highestSurface),
                highestSurfaceZ + 0.5f);
        }

        TerrainRuntimeLogger.Logger.Warning(
            $"Spawn search failed near ({baseX},{baseZ}); using desired position {desiredPosition}.");
        return desiredPosition;
    }

    private static float ComputeSpawnY(int surfaceHeight)
    {
        const float verticalSafetyMargin = 0.15f;
        return (surfaceHeight + 1) + TerrainSceneBootstrapper.PlayerEyeHeightFromFeet + verticalSafetyMargin;
    }

    public void SetBlockWorld(int worldX, int worldY, int worldZ, BlockKind block)
    {
        if (worldY < 0 || worldY >= settings.ChunkHeight)
        {
            TerrainRuntimeLogger.Logger.Warning($"Ignored block edit outside vertical bounds at ({worldX}, {worldY}, {worldZ}).");
            return;
        }

        int chunkX = VoxelGridMath.FloorDiv(worldX, settings.ChunkSize);
        int chunkZ = VoxelGridMath.FloorDiv(worldZ, settings.ChunkSize);
        int localX = VoxelGridMath.PositiveMod(worldX, settings.ChunkSize);
        int localZ = VoxelGridMath.PositiveMod(worldZ, settings.ChunkSize);
        var coordinate = new VoxelChunkCoordinate(chunkX, chunkZ);

        overrideStore.SetOverride(coordinate, settings.ChunkSize, localX, worldY, localZ, block);
        TerrainRuntimeLogger.Logger.Info($"Block override set at world=({worldX},{worldY},{worldZ}) chunk={coordinate} local=({localX},{worldY},{localZ}) value={block}.");

        QueueChunkAndNeighborRefresh(coordinate, localX, localZ);
    }

    public void RefreshVisibleChunks(Scene scene, Vector3 cameraPosition, bool force)
    {
        if (force)
        {
            Stats.VisibleFaceCount = 0;
            Stats.SolidFaceCount = 0;
            Stats.WaterFaceCount = 0;
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
                runtime.VisualEntity.Scene = null;
                if (runtime.CollisionEntity is not null)
                {
                    runtime.CollisionEntity.Scene = null;
                }
                Stats.VisibleFaceCount -= runtime.MeshData.FaceCount;
                Stats.SolidFaceCount -= runtime.MeshData.Solid.FaceCount;
                Stats.WaterFaceCount -= runtime.MeshData.Water.FaceCount;
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
                TerrainRuntimeLogger.Logger.Debug($"Ignored built chunk outside current view {result.Coordinate} rev={result.Revision}.");
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
        int revision = nextRevision++;
        TerrainRuntimeLogger.Logger.Debug($"Requesting chunk build {coordinate} rev={revision}.");
        buildPipeline.Enqueue(coordinate, revision);
    }

    private void QueueChunkAndNeighborRefresh(VoxelChunkCoordinate coordinate, int localX, int localZ)
    {
        RemoveActiveChunk(coordinate);
        RequestChunkBuild(coordinate);

        if (localX == 0)
        {
            RemoveActiveChunk(new VoxelChunkCoordinate(coordinate.X - 1, coordinate.Z));
            RequestChunkBuild(new VoxelChunkCoordinate(coordinate.X - 1, coordinate.Z));
        }
        else if (localX == settings.ChunkSize - 1)
        {
            RemoveActiveChunk(new VoxelChunkCoordinate(coordinate.X + 1, coordinate.Z));
            RequestChunkBuild(new VoxelChunkCoordinate(coordinate.X + 1, coordinate.Z));
        }

        if (localZ == 0)
        {
            RemoveActiveChunk(new VoxelChunkCoordinate(coordinate.X, coordinate.Z - 1));
            RequestChunkBuild(new VoxelChunkCoordinate(coordinate.X, coordinate.Z - 1));
        }
        else if (localZ == settings.ChunkSize - 1)
        {
            RemoveActiveChunk(new VoxelChunkCoordinate(coordinate.X, coordinate.Z + 1));
            RequestChunkBuild(new VoxelChunkCoordinate(coordinate.X, coordinate.Z + 1));
        }
    }

    private bool HasStandingClearance(int worldX, int surfaceHeight, int worldZ)
    {
        int feetY = surfaceHeight + 1;
        int headY = surfaceHeight + 2;
        int upperHeadY = surfaceHeight + 3;

        return GetBlockWorld(worldX, feetY, worldZ) == BlockKind.Air
            && GetBlockWorld(worldX, headY, worldZ) == BlockKind.Air
            && GetBlockWorld(worldX, upperHeadY, worldZ) == BlockKind.Air;
    }

    private BlockKind GetBlockWorld(int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= settings.ChunkHeight)
        {
            return BlockKind.Air;
        }

        int chunkX = VoxelGridMath.FloorDiv(worldX, settings.ChunkSize);
        int chunkZ = VoxelGridMath.FloorDiv(worldZ, settings.ChunkSize);
        int localX = VoxelGridMath.PositiveMod(worldX, settings.ChunkSize);
        int localZ = VoxelGridMath.PositiveMod(worldZ, settings.ChunkSize);
        var coordinate = new VoxelChunkCoordinate(chunkX, chunkZ);

        if (chunks.TryGetValue(coordinate, out VoxelChunkRuntime? runtime))
        {
            return runtime.Data.GetBlock(localX, worldY, localZ);
        }

        VoxelChunkData sampledChunk = generator.Generate(coordinate);
        return sampledChunk.GetBlock(localX, worldY, localZ);
    }

    public BlockKind SampleBlockWorld(int worldX, int worldY, int worldZ)
    {
        return GetBlockWorld(worldX, worldY, worldZ);
    }

    private void RemoveActiveChunk(VoxelChunkCoordinate coordinate)
    {
        if (chunks.Remove(coordinate, out VoxelChunkRuntime? existing))
        {
            Stats.VisibleFaceCount -= existing.MeshData.FaceCount;
            Stats.SolidFaceCount -= existing.MeshData.Solid.FaceCount;
            Stats.WaterFaceCount -= existing.MeshData.Water.FaceCount;
            existing.VisualEntity.Scene = null;
            if (existing.CollisionEntity is not null)
            {
                existing.CollisionEntity.Scene = null;
            }
            TerrainRuntimeLogger.Logger.Debug($"Removed active chunk {coordinate}.");
        }
    }

    private void IntegrateChunk(Scene scene, ChunkBuildResult result)
    {
        RemoveActiveChunk(result.Coordinate);

        Vector3 chunkWorldPosition = new(
            result.Coordinate.X * settings.ChunkSize * settings.VoxelScale,
            0f,
            result.Coordinate.Z * settings.ChunkSize * settings.VoxelScale);

        var visualEntity = new Entity($"Chunk_{result.Coordinate.X}_{result.Coordinate.Z}");
        visualEntity.Transform.Position = chunkWorldPosition;
        modelFactory.AttachModels(visualEntity, result.MeshData);
        scene.Entities.Add(visualEntity);

        Entity? collisionEntity = null;
        if (!result.CollisionData.IsEmpty)
        {
            collisionEntity = new Entity($"ChunkCollision_{result.Coordinate.X}_{result.Coordinate.Z}");
            collisionEntity.Transform.Position = chunkWorldPosition;
            modelFactory.AttachCollision(collisionEntity, result.CollisionData);
            scene.Entities.Add(collisionEntity);
        }

        chunks[result.Coordinate] = new VoxelChunkRuntime(result.Coordinate, result.Data, result.MeshData, result.CollisionData, visualEntity, collisionEntity);
        Stats.VisibleFaceCount += result.MeshData.FaceCount;
        Stats.SolidFaceCount += result.MeshData.Solid.FaceCount;
        Stats.WaterFaceCount += result.MeshData.Water.FaceCount;
        TerrainRuntimeLogger.Logger.Debug($"Integrated chunk {result.Coordinate} rev={result.Revision}, collisionBoxes={result.CollisionData.Boxes.Count}.");
    }

    private ChunkBuildResult BuildChunkImmediate(VoxelChunkCoordinate coordinate)
    {
        TerrainRuntimeLogger.Logger.Info($"Warmup-building spawn chunk {coordinate} synchronously.");
        VoxelChunkData data = generator.Generate(coordinate);
        VoxelChunkMeshData meshData = mesher.Build(data, generator.SampleBlockWorld);
        VoxelChunkCollisionData collisionData = collisionBuilder.Build(data);
        return new ChunkBuildResult(nextRevision++, coordinate, data, meshData, collisionData);
    }
}
