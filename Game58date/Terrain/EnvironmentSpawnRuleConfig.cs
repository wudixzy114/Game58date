#nullable enable
using System.Text.Json.Serialization;

namespace Game58date.Terrain;

public sealed class EnvironmentSpawnRuleConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("primaryBiome")]
    public BiomeKind PrimaryBiome { get; set; }

    [JsonPropertyName("weight")]
    public float Weight { get; set; } = 1f;

    [JsonPropertyName("probabilityThreshold")]
    public float ProbabilityThreshold { get; set; } = 0.5f;

    [JsonPropertyName("maxPlacementsPerChunk")]
    public int MaxPlacementsPerChunk { get; set; } = 1;

    [JsonPropertyName("gridStep")]
    public int GridStep { get; set; } = 4;

    [JsonPropertyName("minEdgePadding")]
    public int MinEdgePadding { get; set; } = 2;

    [JsonPropertyName("clearanceHeight")]
    public int ClearanceHeight { get; set; } = 4;

    [JsonPropertyName("minSlope")]
    public float MinSlope { get; set; }

    [JsonPropertyName("maxSlope")]
    public float MaxSlope { get; set; } = 1f;

    [JsonPropertyName("minMoisture")]
    public float MinMoisture { get; set; }

    [JsonPropertyName("maxMoisture")]
    public float MaxMoisture { get; set; } = 1f;

    [JsonPropertyName("minTemperature")]
    public float MinTemperature { get; set; }

    [JsonPropertyName("maxTemperature")]
    public float MaxTemperature { get; set; } = 1f;

    [JsonPropertyName("requiresSoftGround")]
    public bool RequiresSoftGround { get; set; }

    [JsonPropertyName("allowsWaterAdjacency")]
    public bool AllowsWaterAdjacency { get; set; }

    [JsonPropertyName("nearVariant")]
    public EnvironmentPropVariantConfig NearVariant { get; set; } = new();

    [JsonPropertyName("midVariant")]
    public EnvironmentPropVariantConfig MidVariant { get; set; } = new();

    [JsonPropertyName("farVariant")]
    public EnvironmentPropVariantConfig? FarVariant { get; set; }
}
