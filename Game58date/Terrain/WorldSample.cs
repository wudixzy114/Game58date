namespace Game58date.Terrain;

public readonly record struct WorldSample(
    int SurfaceHeight,
    int StoneHeight,
    int WaterLevel,
    float CaveDensity,
    BiomeKind Biome,
    float Moisture,
    float Temperature,
    float Continentalness,
    float RidgeStrength);
