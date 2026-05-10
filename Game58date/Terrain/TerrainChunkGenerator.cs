#nullable enable
using System.Collections.Generic;

namespace Game58date.Terrain;

public sealed class TerrainChunkGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly WorldFieldSampler sampler;
    private readonly SurfaceMaterialResolver surfaceMaterialResolver;
    private readonly VoxelChunkOverrideStore overrideStore;

    public TerrainChunkGenerator(TerrainGenerationSettings settings, VoxelChunkOverrideStore overrideStore)
    {
        this.settings = settings;
        this.overrideStore = overrideStore;
        sampler = new WorldFieldSampler(settings);
        surfaceMaterialResolver = new SurfaceMaterialResolver(settings);
    }

    public VoxelChunkData Generate(VoxelChunkCoordinate coordinate)
    {
        var chunk = new VoxelChunkData(coordinate, settings.ChunkSize, settings.ChunkHeight);
        int worldStartX = coordinate.X * settings.ChunkSize;
        int worldStartZ = coordinate.Z * settings.ChunkSize;

        for (int localZ = 0; localZ < settings.ChunkSize; localZ++)
        {
            int worldZ = worldStartZ + localZ;
            for (int localX = 0; localX < settings.ChunkSize; localX++)
            {
                int worldX = worldStartX + localX;
                WorldSample sample = sampler.SampleSurface(worldX, worldZ);
                FillColumn(chunk, localX, localZ, worldX, worldZ, sample);
            }
        }

        ApplyOverrides(chunk);

        return chunk;
    }

    public WorldSample SampleSurfaceWorld(int worldX, int worldZ)
    {
        return sampler.SampleSurface(worldX, worldZ);
    }

    public BlockKind SampleBlockWorld(int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= settings.ChunkHeight)
        {
            return BlockKind.Air;
        }

        WorldSample sample = sampler.SampleSurface(worldX, worldZ);
        BlockKind block = ResolveBlockKind(worldX, worldY, worldZ, sample);

        int chunkX = VoxelGridMath.FloorDiv(worldX, settings.ChunkSize);
        int chunkZ = VoxelGridMath.FloorDiv(worldZ, settings.ChunkSize);
        if (!overrideStore.TryGetChunkOverrides(new VoxelChunkCoordinate(chunkX, chunkZ), out IReadOnlyDictionary<int, BlockKind>? chunkOverrides) || chunkOverrides is null)
        {
            return block;
        }

        int localX = VoxelGridMath.PositiveMod(worldX, settings.ChunkSize);
        int localZ = VoxelGridMath.PositiveMod(worldZ, settings.ChunkSize);
        int index = localX + settings.ChunkSize * (localZ + settings.ChunkSize * worldY);
        return chunkOverrides.TryGetValue(index, out BlockKind overriddenBlock)
            ? overriddenBlock
            : block;
    }

    private void FillColumn(VoxelChunkData chunk, int localX, int localZ, int worldX, int worldZ, WorldSample sample)
    {
        for (int y = 0; y < chunk.Height; y++)
        {
            BlockKind block = ResolveBlockKind(worldX, y, worldZ, sample);
            chunk.SetBlock(localX, y, localZ, block);
        }
    }

    private BlockKind ResolveBlockKind(int worldX, int worldY, int worldZ, WorldSample sample)
    {
        if (worldY == 0)
        {
            return BlockKind.Bedrock;
        }

        bool solid = sampler.SampleTerrainDensity(worldX, worldY, worldZ, sample) > 0f;
        if (!solid)
        {
            return worldY <= sample.WaterLevel ? BlockKind.Water : BlockKind.Air;
        }

        bool openAbove = sampler.SampleTerrainDensity(worldX, worldY + 1, worldZ, sample) <= 0f;
        bool openSide =
            sampler.SampleTerrainDensity(worldX + 1, worldY, worldZ, sample) <= 0f ||
            sampler.SampleTerrainDensity(worldX - 1, worldY, worldZ, sample) <= 0f ||
            sampler.SampleTerrainDensity(worldX, worldY, worldZ + 1, sample) <= 0f ||
            sampler.SampleTerrainDensity(worldX, worldY, worldZ - 1, sample) <= 0f;

        bool exposed = openAbove || openSide;
        int depthFromSurface = sample.SurfaceHeight - worldY;

        if (!exposed)
        {
            return worldY > sample.StoneHeight
                ? ResolveSubsurfaceBlock(sample, worldY)
                : ResolveDeepBlock(sample);
        }

        SurfaceMaterialKind materialKind = surfaceMaterialResolver.Resolve(sample, worldY, openAbove);
        switch (materialKind)
        {
            case SurfaceMaterialKind.Shore:
                return BlockKind.Sand;

            case SurfaceMaterialKind.Wetland:
                return openAbove ? BlockKind.Mud : BlockKind.Peat;

            case SurfaceMaterialKind.ForestFloor:
                return openAbove ? BlockKind.Moss : BlockKind.Dirt;

            case SurfaceMaterialKind.Cliff:
                return depthFromSurface <= 2 ? BlockKind.Stone : BlockKind.Dirt;

            case SurfaceMaterialKind.Scree:
                return BlockKind.Scree;

            case SurfaceMaterialKind.Alpine:
                return openAbove ? BlockKind.Snow : BlockKind.Stone;

            case SurfaceMaterialKind.HighGrass:
                return openAbove ? BlockKind.Grass : BlockKind.Dirt;

            default:
                if (openAbove)
                {
                    return BlockKind.Grass;
                }

                if (depthFromSurface <= 4)
                {
                    return BlockKind.Dirt;
                }

                return BlockKind.Stone;
        }
    }

    private static BlockKind ResolveSubsurfaceBlock(WorldSample sample, int worldY)
    {
        if (sample.Biome == BiomeKind.Shore)
        {
            return BlockKind.Sand;
        }

        if (sample.Biome == BiomeKind.Wetland)
        {
            return worldY >= sample.WaterLevel - 1
                ? BlockKind.Peat
                : BlockKind.Dirt;
        }

        if (sample.Biome == BiomeKind.Woodland)
        {
            return BlockKind.Dirt;
        }

        if (sample.Biome == BiomeKind.Scree)
        {
            return BlockKind.Scree;
        }

        if (sample.Biome == BiomeKind.Alpine)
        {
            return BlockKind.Stone;
        }

        return BlockKind.Dirt;
    }

    private static BlockKind ResolveDeepBlock(WorldSample sample)
    {
        return sample.Biome == BiomeKind.Scree
            ? BlockKind.Scree
            : BlockKind.Stone;
    }

    private void ApplyOverrides(VoxelChunkData chunk)
    {
        if (!overrideStore.TryGetChunkOverrides(chunk.Coordinate, out IReadOnlyDictionary<int, BlockKind>? chunkOverrides) || chunkOverrides is null)
        {
            return;
        }

        foreach ((int index, BlockKind block) in chunkOverrides)
        {
            int y = index / (chunk.Size * chunk.Size);
            int remainder = index - y * chunk.Size * chunk.Size;
            int z = remainder / chunk.Size;
            int x = remainder - z * chunk.Size;
            chunk.SetBlock(x, y, z, block);
        }
    }
}
