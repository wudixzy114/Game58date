using System;
using Game58date.Terrain.Noise;

namespace Game58date.Terrain;

public sealed class WorldFieldSampler
{
    private readonly TerrainGenerationSettings settings;
    private readonly DeterministicNoise continentalNoise;
    private readonly DeterministicNoise erosionNoise;
    private readonly DeterministicNoise ridgeNoise;
    private readonly DeterministicNoise moistureNoise;
    private readonly DeterministicNoise temperatureNoise;
    private readonly DeterministicNoise caveNoiseA;
    private readonly DeterministicNoise caveNoiseB;

    public WorldFieldSampler(TerrainGenerationSettings settings)
    {
        this.settings = settings;
        continentalNoise = new DeterministicNoise(settings.Seed);
        erosionNoise = new DeterministicNoise(settings.Seed * 31 + 17);
        ridgeNoise = new DeterministicNoise(settings.Seed * 131 + 59);
        moistureNoise = new DeterministicNoise(settings.Seed * 211 + 83);
        temperatureNoise = new DeterministicNoise(settings.Seed * 353 + 101);
        caveNoiseA = new DeterministicNoise(settings.Seed * 557 + 211);
        caveNoiseB = new DeterministicNoise(settings.Seed * 919 + 431);
    }

    public WorldSample SampleSurface(int worldX, int worldZ)
    {
        float continentalness = continentalNoise.Fractal2D(worldX, worldZ, 5, 0.0045f, 0.5f, 2.0f);
        float erosion = erosionNoise.Fractal2D(worldX, worldZ, 4, 0.012f, 0.55f, 2.1f);
        float ridge = 1f - MathF.Abs(ridgeNoise.Fractal2D(worldX, worldZ, 4, 0.008f, 0.5f, 2.0f));
        float moisture = Normalize(moistureNoise.Fractal2D(worldX, worldZ, 4, 0.003f, 0.5f, 2.0f));
        float temperature = Normalize(temperatureNoise.Fractal2D(worldX, worldZ, 3, 0.0025f, 0.5f, 2.0f));

        float coastalMask = Saturate((0.14f - MathF.Abs(continentalness)) * 5.0f);
        float plainMask = Saturate(1f - coastalMask - MathF.Max(0f, ridge - 0.45f) * 1.35f);
        float mountainMask = Saturate(MathF.Max(0f, continentalness - 0.04f) * 1.35f);

        float baseLand = settings.BaseHeight + continentalness * settings.HeightAmplitude;
        float mountainHeight = ridge * ridge * settings.MountainAmplitude * mountainMask;
        float erosionCut = erosion * 5.5f;
        float coastFlatten = coastalMask * 4.0f;
        float rollingHills = plainMask * ridge * 3.5f;

        int surfaceHeight = (int)MathF.Round(baseLand + mountainHeight + rollingHills - erosionCut - coastFlatten);
        surfaceHeight = Math.Clamp(surfaceHeight, 3, settings.ChunkHeight - 2);

        int soilDepth = 4 + (int)MathF.Round(moisture * 2f);
        int stoneHeight = Math.Max(1, surfaceHeight - soilDepth);

        return new WorldSample(
            surfaceHeight,
            stoneHeight,
            settings.WaterLevel,
            0f,
            ResolveBiome(coastalMask, mountainMask, ridge),
            moisture,
            temperature,
            continentalness,
            ridge);
    }

    public float SampleCaveDensity(int worldX, int worldY, int worldZ)
    {
        float primary = SamplePseudo3D(caveNoiseA, worldX, worldY, worldZ, 0.026f);
        float secondary = SamplePseudo3D(caveNoiseB, worldX, worldY, worldZ, 0.061f);
        float verticalMask = Saturate((worldY - 8f) / (settings.ChunkHeight * 0.82f));
        return primary * 0.75f + secondary * 0.25f - verticalMask * 0.12f;
    }

    private static BiomeKind ResolveBiome(float coastalMask, float mountainMask, float ridge)
    {
        if (coastalMask > 0.55f)
        {
            return BiomeKind.Shore;
        }

        if (mountainMask > 0.52f && ridge > 0.58f)
        {
            return BiomeKind.Mountains;
        }

        if (ridge > 0.44f)
        {
            return BiomeKind.Hills;
        }

        return BiomeKind.Plains;
    }

    private static float SamplePseudo3D(DeterministicNoise noise, int x, int y, int z, float frequency)
    {
        float xy = noise.Sample2D(x * frequency + y * 0.173f, z * frequency + y * 0.137f);
        float yz = noise.Sample2D(y * frequency + z * 0.197f, x * frequency + z * 0.113f);
        float xz = noise.Sample2D(x * frequency, z * frequency);
        return (xy + yz + xz) / 3f;
    }

    private static float Normalize(float value)
    {
        return value * 0.5f + 0.5f;
    }

    private static float Saturate(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}
