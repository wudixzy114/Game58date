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
    private readonly DeterministicNoise warpNoiseX;
    private readonly DeterministicNoise warpNoiseZ;
    private readonly DeterministicNoise overhangNoiseA;
    private readonly DeterministicNoise overhangNoiseB;
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
        warpNoiseX = new DeterministicNoise(settings.Seed * 461 + 137);
        warpNoiseZ = new DeterministicNoise(settings.Seed * 503 + 173);
        overhangNoiseA = new DeterministicNoise(settings.Seed * 613 + 199);
        overhangNoiseB = new DeterministicNoise(settings.Seed * 733 + 223);
        caveNoiseA = new DeterministicNoise(settings.Seed * 557 + 211);
        caveNoiseB = new DeterministicNoise(settings.Seed * 919 + 431);
    }

    public WorldSample SampleSurface(int worldX, int worldZ)
    {
        float surfaceBase = SampleSurfaceHeightValue(worldX, worldZ, out float continentalness, out float ridge, out float moisture, out float temperature, out float shoreWeight, out float hillWeight, out float mountainWeight);
        int surfaceHeight = Math.Clamp((int)MathF.Round(surfaceBase), 3, settings.ChunkHeight - 2);
        surfaceHeight = Math.Clamp(surfaceHeight, 3, settings.ChunkHeight - 2);

        int soilDepth = 4 + (int)MathF.Round(moisture * 2f);
        int stoneHeight = Math.Max(1, surfaceHeight - soilDepth);
        float slope = EstimateSlope(worldX, worldZ);

        return new WorldSample(
            surfaceHeight,
            stoneHeight,
            settings.WaterLevel,
            ResolveBiome(shoreWeight, mountainWeight, ridge),
            moisture,
            temperature,
            continentalness,
            ridge,
            shoreWeight,
            hillWeight,
            mountainWeight,
            slope);
    }

    public float SampleCaveDensity(int worldX, int worldY, int worldZ)
    {
        float primary = SamplePseudo3D(caveNoiseA, worldX, worldY, worldZ, 0.026f);
        float secondary = SamplePseudo3D(caveNoiseB, worldX, worldY, worldZ, 0.061f);
        float verticalMask = Saturate((worldY - 8f) / (settings.ChunkHeight * 0.82f));
        return primary * 0.75f + secondary * 0.25f - verticalMask * 0.12f;
    }

    public float SampleTerrainDensity(int worldX, int worldY, int worldZ, WorldSample sample)
    {
        float density = sample.SurfaceHeight - worldY;
        float surfaceBand = Saturate(1f - MathF.Abs(worldY - (sample.SurfaceHeight - 3f)) / settings.SurfaceBandHeight);

        float overhangA = SamplePseudo3D(overhangNoiseA, worldX, worldY, worldZ, 0.019f);
        float overhangB = SamplePseudo3D(overhangNoiseB, worldX, worldY, worldZ, 0.033f);
        float overhang = overhangA * 0.72f + overhangB * 0.28f;
        float overhangContribution = overhang * sample.MountainWeight * surfaceBand * settings.OverhangStrength;
        float cliffContribution = overhangB * sample.HillWeight * sample.Slope * surfaceBand * (settings.OverhangStrength * 0.45f);

        float cave = SampleCaveDensity(worldX, worldY, worldZ);
        float caveCarving = MathF.Max(0f, cave - settings.CaveThreshold) * 18f;

        return density + overhangContribution + cliffContribution - caveCarving;
    }

    private float SampleSurfaceHeightValue(
        int worldX,
        int worldZ,
        out float continentalness,
        out float ridge,
        out float moisture,
        out float temperature,
        out float shoreWeight,
        out float hillWeight,
        out float mountainWeight)
    {
        float warpedX = worldX + warpNoiseX.Fractal2D(worldX, worldZ, 3, settings.DomainWarpFrequency, 0.5f, 2.0f) * settings.DomainWarpAmplitude;
        float warpedZ = worldZ + warpNoiseZ.Fractal2D(worldX, worldZ, 3, settings.DomainWarpFrequency, 0.5f, 2.0f) * settings.DomainWarpAmplitude;

        continentalness = continentalNoise.Fractal2D(warpedX, warpedZ, 5, 0.0045f, 0.5f, 2.0f);
        float erosion = erosionNoise.Fractal2D(warpedX, warpedZ, 4, 0.012f, 0.55f, 2.1f);
        ridge = 1f - MathF.Abs(ridgeNoise.Fractal2D(warpedX, warpedZ, 4, 0.008f, 0.5f, 2.0f));
        moisture = Normalize(moistureNoise.Fractal2D(warpedX, warpedZ, 4, 0.003f, 0.5f, 2.0f));
        temperature = Normalize(temperatureNoise.Fractal2D(warpedX, warpedZ, 3, 0.0025f, 0.5f, 2.0f));

        shoreWeight = Saturate((0.16f - MathF.Abs(continentalness)) * 5.5f);
        mountainWeight = Saturate(MathF.Max(0f, continentalness - 0.03f) * 1.55f);
        hillWeight = Saturate((ridge - 0.34f) * 1.45f) * (1f - mountainWeight * 0.65f);
        float plainWeight = Saturate(1f - shoreWeight - hillWeight * 0.6f - mountainWeight);

        float baseLand = settings.BaseHeight + continentalness * settings.HeightAmplitude;
        float erosionCut = erosion * 5.5f;
        float coastFlatten = shoreWeight * 5.0f;
        float rollingHills = ridge * hillWeight * 7.5f;
        float mountainHeight = ridge * ridge * settings.MountainAmplitude * mountainWeight;
        float terrace = ApplyTerracing(baseLand + mountainHeight + rollingHills - erosionCut - coastFlatten, 2.0f, 0.28f);
        float detail = ridge * plainWeight * 1.5f;
        return terrace + detail;
    }

    private float EstimateSlope(int worldX, int worldZ)
    {
        float center = SampleSurfaceHeightValue(worldX, worldZ, out _, out _, out _, out _, out _, out _, out _);
        float offsetX = SampleSurfaceHeightValue(worldX + 2, worldZ, out _, out _, out _, out _, out _, out _, out _);
        float offsetZ = SampleSurfaceHeightValue(worldX, worldZ + 2, out _, out _, out _, out _, out _, out _, out _);
        float dx = MathF.Abs(offsetX - center);
        float dz = MathF.Abs(offsetZ - center);
        return Saturate((dx + dz) / 6f);
    }

    private static BiomeKind ResolveBiome(float shoreWeight, float mountainWeight, float ridge)
    {
        if (shoreWeight > 0.55f)
        {
            return BiomeKind.Shore;
        }

        if (mountainWeight > 0.48f && ridge > 0.55f)
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

    private static float ApplyTerracing(float value, float stepHeight, float blend)
    {
        float stepped = MathF.Floor(value / stepHeight) * stepHeight;
        return stepped + (value - stepped) * blend;
    }

    private static float Saturate(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}
