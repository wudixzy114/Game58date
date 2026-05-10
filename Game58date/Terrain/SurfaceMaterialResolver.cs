using System;

namespace Game58date.Terrain;

public sealed class SurfaceMaterialResolver
{
    private readonly TerrainGenerationSettings settings;

    public SurfaceMaterialResolver(TerrainGenerationSettings settings)
    {
        this.settings = settings;
    }

    public SurfaceMaterialKind Resolve(WorldSample sample, int worldY, bool openAbove)
    {
        float shoreInfluence = MathF.Max(sample.Weights.Shore, sample.ShoreWeight);
        float wetlandInfluence = MathF.Max(sample.Weights.Wetland, sample.WetlandWeight * 0.85f);
        float woodlandInfluence = MathF.Max(sample.Weights.Woodland, sample.WoodlandWeight * 0.82f) * (1f - sample.TreeLine * 0.75f);
        float screeInfluence = MathF.Max(sample.Weights.Scree, sample.Slope * 0.54f + sample.Transition * 0.10f);
        float alpineInfluence = MathF.Max(sample.Weights.Alpine, sample.SnowCoverMask * 0.72f + sample.TreeLine * 0.16f);

        if (worldY <= sample.WaterLevel + 1 || shoreInfluence > 0.32f)
        {
            return SurfaceMaterialKind.Shore;
        }

        if (wetlandInfluence > 0.42f || wetlandInfluence > 0.28f && sample.Moisture > 0.70f && sample.Slope < 0.22f)
        {
            return SurfaceMaterialKind.Wetland;
        }

        if (alpineInfluence > 0.46f || sample.Elevation > 0.58f && sample.SnowCoverMask > 0.58f)
        {
            return SurfaceMaterialKind.Alpine;
        }

        if (screeInfluence > 0.42f && sample.Moisture < 0.58f)
        {
            return SurfaceMaterialKind.Scree;
        }

        if (sample.Weights.Mountains > 0.38f || sample.Slope > settings.SteepSlopeThreshold - sample.Transition * 0.04f)
        {
            return SurfaceMaterialKind.Cliff;
        }

        if (woodlandInfluence > 0.34f && sample.Moisture > 0.42f)
        {
            return SurfaceMaterialKind.ForestFloor;
        }

        if (openAbove && sample.Weights.Plains > 0.42f && sample.Moisture > 0.44f && sample.TreeLine < 0.52f)
        {
            return SurfaceMaterialKind.HighGrass;
        }

        return SurfaceMaterialKind.Soil;
    }
}
