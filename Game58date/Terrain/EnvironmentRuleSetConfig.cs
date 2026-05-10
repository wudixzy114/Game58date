#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Game58date.Terrain;

public sealed class EnvironmentRuleSetConfig
{
    [JsonPropertyName("vegetationRules")]
    public List<EnvironmentSpawnRuleConfig> VegetationRules { get; set; } = new();

    [JsonPropertyName("structureRules")]
    public List<EnvironmentSpawnRuleConfig> StructureRules { get; set; } = new();

    [JsonPropertyName("animalRules")]
    public List<EnvironmentSpawnRuleConfig> AnimalRules { get; set; } = new();
}
