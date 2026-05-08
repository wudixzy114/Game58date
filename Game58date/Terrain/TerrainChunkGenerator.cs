using Game58date.Terrain.Noise;

namespace Game58date.Terrain;

public sealed class TerrainChunkGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly DeterministicNoise continentalNoise;
    private readonly DeterministicNoise erosionNoise;
    private readonly DeterministicNoise ridgeNoise;

    public TerrainChunkGenerator(TerrainGenerationSettings settings)
    {
        this.settings = settings;
        continentalNoise = new DeterministicNoise(settings.Seed);
        erosionNoise = new DeterministicNoise(settings.Seed * 31 + 17);
        ridgeNoise = new DeterministicNoise(settings.Seed * 131 + 59);
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
                int terrainHeight = SampleTerrainHeight(worldX, worldZ);
                FillColumn(chunk, localX, localZ, terrainHeight);
            }
        }

        return chunk;
    }

    private int SampleTerrainHeight(int worldX, int worldZ)
    {
        float continental = continentalNoise.Fractal2D(worldX, worldZ, 5, 0.0045f, 0.5f, 2.0f);
        float erosion = erosionNoise.Fractal2D(worldX, worldZ, 4, 0.012f, 0.55f, 2.1f);
        float ridges = 1f - System.MathF.Abs(ridgeNoise.Fractal2D(worldX, worldZ, 4, 0.008f, 0.5f, 2.0f));

        float baseLand = settings.BaseHeight + continental * settings.HeightAmplitude;
        float mountainMask = System.MathF.Max(0f, continental - 0.05f) * 1.35f;
        float mountainHeight = ridges * ridges * settings.MountainAmplitude * mountainMask;
        float erosionCut = erosion * 5.5f;
        float total = baseLand + mountainHeight - erosionCut;

        int height = (int)System.MathF.Round(total);
        return System.Math.Clamp(height, 3, settings.ChunkHeight - 2);
    }

    private void FillColumn(VoxelChunkData chunk, int localX, int localZ, int terrainHeight)
    {
        for (int y = 0; y < chunk.Height; y++)
        {
            BlockKind block = ResolveBlockKind(y, terrainHeight);
            chunk.SetBlock(localX, y, localZ, block);
        }
    }

    private BlockKind ResolveBlockKind(int y, int terrainHeight)
    {
        if (y == 0)
        {
            return BlockKind.Bedrock;
        }

        if (y > terrainHeight)
        {
            return y <= settings.WaterLevel ? BlockKind.Water : BlockKind.Air;
        }

        if (y == terrainHeight)
        {
            if (terrainHeight <= settings.WaterLevel + 1)
            {
                return BlockKind.Sand;
            }

            return BlockKind.Grass;
        }

        if (y >= terrainHeight - 3)
        {
            return terrainHeight <= settings.WaterLevel + 1 ? BlockKind.Sand : BlockKind.Dirt;
        }

        return BlockKind.Stone;
    }
}
