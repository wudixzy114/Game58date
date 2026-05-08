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

        if (sample.Weights.Mountains > 0.38f || sample.Slope > settings.SteepSlopeThreshold)
        {
            return SurfaceMaterialKind.Cliff;
        }

        if (openAbove && sample.Weights.Plains > 0.45f && sample.Moisture > 0.45f)
        {
            return SurfaceMaterialKind.HighGrass;
        }

        return SurfaceMaterialKind.Soil;
    }
}
