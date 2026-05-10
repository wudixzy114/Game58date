#nullable enable

namespace Game58date.Terrain;

public readonly record struct EnvironmentSpawnRule(
    string Name,
    BiomeKind PrimaryBiome,
    float Weight,
    float ProbabilityThreshold,
    int MaxPlacementsPerChunk,
    int GridStep,
    int MinEdgePadding,
    int ClearanceHeight,
    float MinSlope,
    float MaxSlope,
    float MinMoisture,
    float MaxMoisture,
    float MinTemperature,
    float MaxTemperature,
    bool RequiresSoftGround,
    bool AllowsWaterAdjacency,
    EnvironmentPropVariant NearVariant,
    EnvironmentPropVariant MidVariant,
    EnvironmentPropVariant? FarVariant = null);
