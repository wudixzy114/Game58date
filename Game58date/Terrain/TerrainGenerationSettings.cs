namespace Game58date.Terrain;

public sealed class TerrainGenerationSettings
{
    public int ChunkSize { get; init; } = 24;

    public int ChunkHeight { get; init; } = 96;

    public int ViewDistanceInChunks { get; init; } = 4;

    public float VoxelScale { get; init; } = 1.0f;

    public int Seed { get; init; } = 580013;

    public float BaseHeight { get; init; } = 24f;

    public float HeightAmplitude { get; init; } = 18f;

    public float MountainAmplitude { get; init; } = 26f;

    public int WaterLevel { get; init; } = 20;
}
