namespace Game58date.Terrain;

public sealed class TerrainGenerationSettings
{
    public const int DefaultSeed = 580013;

    public int ChunkSize { get; init; } = 24;

    public int ChunkHeight { get; init; } = 96;

    public int ViewDistanceInChunks { get; init; } = 4;

    public float VoxelScale { get; init; } = 1.0f;

    public int Seed { get; init; } = DefaultSeed;

    public float BaseHeight { get; init; } = 18f;

    public float HeightAmplitude { get; init; } = 11f;

    public float MountainAmplitude { get; init; } = 18f;

    public int WaterLevel { get; init; } = 20;

    public float DomainWarpFrequency { get; init; } = 0.0024f;

    public float DomainWarpAmplitude { get; init; } = 18f;

    public float OverhangStrength { get; init; } = 4.0f;

    public float SurfaceBandHeight { get; init; } = 10f;

    public float SteepSlopeThreshold { get; init; } = 0.42f;

    public int MinimumSurfaceThickness { get; init; } = 10;

    public float CaveCeilingFadeDepth { get; init; } = 14f;

    public float CaveCarvingStrength { get; init; } = 8f;

    public float CaveThreshold { get; init; } = 0.52f;

    public int MaxConcurrentChunkBuilds { get; init; } = 2;

    public int MaxChunkUploadsPerFrame { get; init; } = 2;

    public TerrainGenerationSettings WithSeed(int seed)
    {
        return new TerrainGenerationSettings
        {
            ChunkSize = ChunkSize,
            ChunkHeight = ChunkHeight,
            ViewDistanceInChunks = ViewDistanceInChunks,
            VoxelScale = VoxelScale,
            Seed = seed,
            BaseHeight = BaseHeight,
            HeightAmplitude = HeightAmplitude,
            MountainAmplitude = MountainAmplitude,
            WaterLevel = WaterLevel,
            DomainWarpFrequency = DomainWarpFrequency,
            DomainWarpAmplitude = DomainWarpAmplitude,
            OverhangStrength = OverhangStrength,
            SurfaceBandHeight = SurfaceBandHeight,
            SteepSlopeThreshold = SteepSlopeThreshold,
            MinimumSurfaceThickness = MinimumSurfaceThickness,
            CaveCeilingFadeDepth = CaveCeilingFadeDepth,
            CaveCarvingStrength = CaveCarvingStrength,
            CaveThreshold = CaveThreshold,
            MaxConcurrentChunkBuilds = MaxConcurrentChunkBuilds,
            MaxChunkUploadsPerFrame = MaxChunkUploadsPerFrame,
        };
    }
}
