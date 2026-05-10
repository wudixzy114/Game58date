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
        if (worldY <= sample.WaterLevel + 1 || sample.Weights.Shore > 0.35f)
        {
            return SurfaceMaterialKind.Shore;
        }

        if (sample.Weights.Wetland > 0.34f || sample.WetlandWeight > 0.42f)
        {
            return SurfaceMaterialKind.Wetland;
        }

        if (sample.Weights.Alpine > 0.36f || sample.AlpineWeight > 0.42f)
        {
            return SurfaceMaterialKind.Alpine;
        }

        if (sample.Weights.Scree > 0.34f || sample.ScreeWeight > 0.38f)
        {
            return SurfaceMaterialKind.Scree;
        }

        if (sample.Weights.Mountains > 0.38f || sample.Slope > settings.SteepSlopeThreshold)
        {
            return SurfaceMaterialKind.Cliff;
        }

        if (sample.Weights.Woodland > 0.34f && sample.Moisture > 0.44f)
        {
            return SurfaceMaterialKind.ForestFloor;
        }

        if (openAbove && sample.Weights.Plains > 0.45f && sample.Moisture > 0.45f)
        {
            return SurfaceMaterialKind.HighGrass;
        }

        return SurfaceMaterialKind.Soil;
    }
}
