namespace Game58date.Terrain;

public sealed class TerrainChunkGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly WorldFieldSampler sampler;
    private readonly SurfaceMaterialResolver surfaceMaterialResolver;

    public TerrainChunkGenerator(TerrainGenerationSettings settings)
    {
        this.settings = settings;
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
                ? sample.Biome == BiomeKind.Shore ? BlockKind.Sand : BlockKind.Dirt
                : BlockKind.Stone;
        }

        SurfaceMaterialKind materialKind = surfaceMaterialResolver.Resolve(sample, worldY, openAbove);
        switch (materialKind)
        {
            case SurfaceMaterialKind.Shore:
                return BlockKind.Sand;

            case SurfaceMaterialKind.Cliff:
                return depthFromSurface <= 2 ? BlockKind.Stone : BlockKind.Dirt;

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
}
