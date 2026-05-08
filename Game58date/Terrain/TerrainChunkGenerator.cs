using Game58date.Terrain.Noise;

namespace Game58date.Terrain;

public sealed class TerrainChunkGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly WorldFieldSampler sampler;

    public TerrainChunkGenerator(TerrainGenerationSettings settings)
    {
        this.settings = settings;
        sampler = new WorldFieldSampler(settings);
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

        return chunk;
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

        if (worldY > sample.SurfaceHeight)
        {
            return worldY <= sample.WaterLevel ? BlockKind.Water : BlockKind.Air;
        }

        float caveDensity = sampler.SampleCaveDensity(worldX, worldY, worldZ);
        bool caveCarved = worldY < sample.SurfaceHeight - 4 && caveDensity > settings.CaveThreshold;
        if (caveCarved)
        {
            return worldY <= sample.WaterLevel ? BlockKind.Water : BlockKind.Air;
        }

        if (worldY == sample.SurfaceHeight)
        {
            return sample.Biome == BiomeKind.Shore ? BlockKind.Sand : BlockKind.Grass;
        }

        if (worldY > sample.StoneHeight)
        {
            return sample.Biome == BiomeKind.Shore ? BlockKind.Sand : BlockKind.Dirt;
        }

        return BlockKind.Stone;
    }
}
