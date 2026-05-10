using System;
using Game58date.Terrain.Noise;
using Stride.Core.Mathematics;

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
        float surfaceBase = SampleSurfaceHeightValue(
            worldX,
            worldZ,
            out float continentalness,
            out float ridge,
            out float moisture,
            out float temperature,
            out float shoreWeight,
            out float wetlandWeight,
            out float woodlandWeight,
            out float hillWeight,
            out float screeWeight,
            out float alpineWeight,
            out float mountainWeight);
        int surfaceHeight = Math.Clamp((int)MathF.Round(surfaceBase), 3, settings.ChunkHeight - 2);
        surfaceHeight = Math.Clamp(surfaceHeight, 3, settings.ChunkHeight - 2);

        int soilDepth = 4 + (int)MathF.Round(moisture * 2f);
        int stoneHeight = Math.Max(1, surfaceHeight - soilDepth);
        float slope = EstimateSlope(worldX, worldZ);
        float elevation = Saturate((surfaceHeight - settings.WaterLevel) / MathF.Max(1f, settings.ChunkHeight - settings.WaterLevel - 10f));
        float treeLine = ComputeTreeLineMask(elevation, temperature);
        BiomeWeights weights = RefineBiomeWeights(
            BuildBiomeWeights(shoreWeight, wetlandWeight, woodlandWeight, hillWeight, screeWeight, alpineWeight, mountainWeight),
            elevation,
            slope,
            moisture,
            temperature,
            treeLine);
        float transition = ComputeTransitionStrength(weights);
        float snowCoverMask = ComputeSnowCoverMask(weights, elevation, slope, moisture, temperature, treeLine);

        return new WorldSample(
            surfaceHeight,
            stoneHeight,
            settings.WaterLevel,
            ResolveBiome(weights, ridge),
            weights,
            moisture,
            temperature,
            continentalness,
            ridge,
            shoreWeight,
            wetlandWeight,
            woodlandWeight,
            hillWeight,
            screeWeight,
            alpineWeight,
            mountainWeight,
            slope,
            elevation,
            transition,
            treeLine,
            snowCoverMask);
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

        float caveDepthAllowance = Saturate((sample.SurfaceHeight - settings.MinimumSurfaceThickness - worldY) / settings.CaveCeilingFadeDepth);
        float cave = SampleCaveDensity(worldX, worldY, worldZ);
        float caveCarving = caveDepthAllowance > 0f
            ? MathF.Max(0f, cave - settings.CaveThreshold) * settings.CaveCarvingStrength * caveDepthAllowance
            : 0f;

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
        out float wetlandWeight,
        out float woodlandWeight,
        out float hillWeight,
        out float screeWeight,
        out float alpineWeight,
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
        wetlandWeight = Saturate((moisture - 0.62f) * 1.6f) * Saturate(1f - hillWeight * 1.25f - mountainWeight) * Saturate(0.40f - MathF.Abs(continentalness) * 1.35f);
        woodlandWeight = Saturate((moisture - 0.38f) * 1.25f) * Saturate((temperature - 0.34f) * 1.10f) * Saturate(1f - hillWeight * 0.72f - mountainWeight * 0.82f - wetlandWeight * 0.58f);
        screeWeight = Saturate((hillWeight * 0.62f + mountainWeight * 0.38f) * Saturate((ridge - 0.52f) * 2.2f) * Saturate((1f - moisture) * 1.25f));
        alpineWeight = Saturate((mountainWeight * 0.72f + hillWeight * 0.18f) * Saturate((0.42f - temperature) * 2.2f) * Saturate(continentalness * 1.18f + 0.35f));
        float plainWeight = Saturate(1f - shoreWeight - wetlandWeight - woodlandWeight - hillWeight * 0.5f - screeWeight * 0.4f - alpineWeight * 0.55f - mountainWeight);

        float baseLand = settings.BaseHeight + continentalness * settings.HeightAmplitude;
        float erosionCut = erosion * 5.5f;
        float coastFlatten = shoreWeight * 5.0f;
        float rollingHills = ridge * hillWeight * 5.0f;
        float wetlandBasin = wetlandWeight * (2.2f + moisture * 1.8f);
        float woodlandRise = woodlandWeight * 1.8f;
        float screeRise = screeWeight * 2.0f;
        float alpineRise = alpineWeight * 3.5f;
        float mountainHeight = ridge * ridge * settings.MountainAmplitude * mountainWeight;
        float terrace = ApplyTerracing(baseLand + mountainHeight + rollingHills - erosionCut - coastFlatten, 2.0f, 0.40f);
        float detail = ridge * plainWeight * 0.9f;
        return terrace + detail + woodlandRise + screeRise + alpineRise - wetlandBasin;
    }

    private float EstimateSlope(int worldX, int worldZ)
    {
        float center = SampleSurfaceHeightValue(worldX, worldZ, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        float offsetX = SampleSurfaceHeightValue(worldX + 2, worldZ, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        float offsetZ = SampleSurfaceHeightValue(worldX, worldZ + 2, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        float dx = MathF.Abs(offsetX - center);
        float dz = MathF.Abs(offsetZ - center);
        return Saturate((dx + dz) / 6f);
    }

    private static BiomeWeights BuildBiomeWeights(
        float shoreWeight,
        float wetlandWeight,
        float woodlandWeight,
        float hillWeight,
        float screeWeight,
        float alpineWeight,
        float mountainWeight)
    {
        float mountains = Saturate(mountainWeight);
        float shore = Saturate(shoreWeight * (1f - mountains * 0.7f));
        float wetlands = Saturate(wetlandWeight * (1f - shore * 0.45f - mountains * 0.62f));
        float woodlands = Saturate(woodlandWeight * (1f - shore * 0.25f - wetlands * 0.35f - mountains * 0.30f));
        float alpine = Saturate(alpineWeight * (1f - shore * 0.60f));
        float scree = Saturate(screeWeight * (1f - shore * 0.50f - wetlands * 0.80f));
        float hills = Saturate(hillWeight * (1f - shore * 0.45f - alpine * 0.48f - scree * 0.30f));
        float plains = Saturate(1f - shore - wetlands - woodlands - hills - scree - alpine - mountains);

        float total = shore + plains + wetlands + woodlands + hills + scree + alpine + mountains;
        if (total <= 0f)
        {
            return new BiomeWeights(0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        return new BiomeWeights(
            shore / total,
            plains / total,
            wetlands / total,
            woodlands / total,
            hills / total,
            scree / total,
            alpine / total,
            mountains / total);
    }

    private static BiomeWeights RefineBiomeWeights(
        BiomeWeights weights,
        float elevation,
        float slope,
        float moisture,
        float temperature,
        float treeLine)
    {
        float shore = weights.Shore;
        float wetlands = weights.Wetland * (1f - treeLine * 0.70f);
        float woodlands = weights.Woodland * (1f - MathF.Max(treeLine * 0.84f, slope * 0.32f));
        float alpineBoost = SmoothStep(0.46f, 0.78f, elevation) * SmoothStep(0.62f, 0.24f, temperature);
        float alpine = MathF.Max(weights.Alpine, alpineBoost * (0.38f + treeLine * 0.52f));
        float screeBoost = SmoothStep(0.18f, 0.50f, slope) * (1f - moisture) * (0.28f + elevation * 0.34f);
        float scree = MathF.Max(weights.Scree, screeBoost);
        float hills = weights.Hills * (1f - alpine * 0.24f) + scree * 0.08f;
        float mountains = MathF.Max(weights.Mountains, SmoothStep(0.52f, 0.86f, elevation) * (0.20f + weights.Mountains * 0.80f));
        float plains = weights.Plains * (1f - treeLine * 0.50f) * (1f - slope * 0.12f);

        return NormalizeWeights(shore, plains, wetlands, woodlands, hills, scree, alpine, mountains);
    }

    private static BiomeKind ResolveBiome(BiomeWeights weights, float ridge)
    {
        if (weights.Shore >= weights.Plains &&
            weights.Shore >= weights.Wetland &&
            weights.Shore >= weights.Woodland &&
            weights.Shore >= weights.Hills &&
            weights.Shore >= weights.Scree &&
            weights.Shore >= weights.Alpine &&
            weights.Shore >= weights.Mountains)
        {
            return BiomeKind.Shore;
        }

        if (weights.Wetland >= weights.Plains &&
            weights.Wetland >= weights.Woodland &&
            weights.Wetland >= weights.Hills &&
            weights.Wetland >= weights.Scree &&
            weights.Wetland >= weights.Alpine &&
            weights.Wetland >= weights.Mountains)
        {
            return BiomeKind.Wetland;
        }

        if (weights.Woodland >= weights.Plains &&
            weights.Woodland >= weights.Hills &&
            weights.Woodland >= weights.Scree &&
            weights.Woodland >= weights.Alpine &&
            weights.Woodland >= weights.Mountains)
        {
            return BiomeKind.Woodland;
        }

        if (weights.Alpine >= weights.Scree &&
            weights.Alpine >= weights.Hills &&
            weights.Alpine >= weights.Plains &&
            weights.Alpine >= weights.Mountains)
        {
            return BiomeKind.Alpine;
        }

        if (weights.Scree >= weights.Hills &&
            weights.Scree >= weights.Plains &&
            weights.Scree >= weights.Mountains)
        {
            return BiomeKind.Scree;
        }

        if (weights.Mountains >= weights.Hills && weights.Mountains >= weights.Plains && ridge > 0.50f)
        {
            return BiomeKind.Mountains;
        }

        if (weights.Hills >= weights.Plains)
        {
            return BiomeKind.Hills;
        }

        return BiomeKind.Plains;
    }

    private static float ComputeTreeLineMask(float elevation, float temperature)
    {
        return SmoothStep(0.44f, 0.70f, elevation) * SmoothStep(0.64f, 0.28f, temperature);
    }

    private static float ComputeTransitionStrength(BiomeWeights weights)
    {
        Span<float> values =
        [
            weights.Shore,
            weights.Plains,
            weights.Wetland,
            weights.Woodland,
            weights.Hills,
            weights.Scree,
            weights.Alpine,
            weights.Mountains,
        ];

        float strongest = 0f;
        float secondStrongest = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            float value = values[i];
            if (value >= strongest)
            {
                secondStrongest = strongest;
                strongest = value;
            }
            else if (value > secondStrongest)
            {
                secondStrongest = value;
            }
        }

        if (strongest <= 0f)
        {
            return 0f;
        }

        return Saturate(secondStrongest / strongest);
    }

    private static float ComputeSnowCoverMask(
        BiomeWeights weights,
        float elevation,
        float slope,
        float moisture,
        float temperature,
        float treeLine)
    {
        float coldness = Saturate((0.52f - temperature) * 1.8f);
        float alpineSupport = MathF.Max(weights.Alpine, weights.Mountains * 0.72f);
        float exposure = MathF.Max(slope * 0.32f, treeLine * 0.26f);
        return Saturate(coldness * 0.56f + alpineSupport * 0.34f + elevation * 0.14f + moisture * 0.08f + exposure);
    }

    private static BiomeWeights NormalizeWeights(
        float shore,
        float plains,
        float wetlands,
        float woodlands,
        float hills,
        float scree,
        float alpine,
        float mountains)
    {
        shore = Saturate(shore);
        plains = Saturate(plains);
        wetlands = Saturate(wetlands);
        woodlands = Saturate(woodlands);
        hills = Saturate(hills);
        scree = Saturate(scree);
        alpine = Saturate(alpine);
        mountains = Saturate(mountains);

        float total = shore + plains + wetlands + woodlands + hills + scree + alpine + mountains;
        if (total <= 0f)
        {
            return new BiomeWeights(0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        return new BiomeWeights(
            shore / total,
            plains / total,
            wetlands / total,
            woodlands / total,
            hills / total,
            scree / total,
            alpine / total,
            mountains / total);
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

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        if (MathUtil.NearEqual(edge0, edge1))
        {
            return value >= edge1 ? 1f : 0f;
        }

        float t = Saturate((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static float Saturate(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}
