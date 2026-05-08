namespace Game58date.Terrain;

public readonly record struct WorldSample(
    int SurfaceHeight,
    int StoneHeight,
    int WaterLevel,
    BiomeKind Biome,
    float Moisture,
    float Temperature,
    float Continentalness,
    float RidgeStrength,
    float ShoreWeight,
    float HillWeight,
    float MountainWeight,
    float Slope);
