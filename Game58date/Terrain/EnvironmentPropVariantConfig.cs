#nullable enable
using System.Text.Json.Serialization;
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class EnvironmentPropVariantConfig
{
    [JsonPropertyName("kind")]
    public EnvironmentPropKind Kind { get; set; }

    [JsonPropertyName("lod")]
    public EnvironmentLodLevel Lod { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("assetKey")]
    public string AssetKey { get; set; } = string.Empty;

    [JsonPropertyName("fallbackMaterial")]
    public EnvironmentMaterialKind? FallbackMaterial { get; set; }

    [JsonPropertyName("usesPlaceholderGeometry")]
    public bool? UsesPlaceholderGeometry { get; set; }

    [JsonPropertyName("radius")]
    public float Radius { get; set; } = 1f;

    [JsonPropertyName("heightScale")]
    public float HeightScale { get; set; } = 1f;

    [JsonPropertyName("densityScale")]
    public float DensityScale { get; set; } = 1f;

    [JsonPropertyName("requiresSoftGround")]
    public bool RequiresSoftGround { get; set; }

    [JsonPropertyName("requiresOpenSky")]
    public bool RequiresOpenSky { get; set; }

    [JsonPropertyName("minSlope")]
    public float MinSlope { get; set; }

    [JsonPropertyName("maxSlope")]
    public float MaxSlope { get; set; } = 1f;

    [JsonPropertyName("minTemperature")]
    public float MinTemperature { get; set; }

    [JsonPropertyName("maxTemperature")]
    public float MaxTemperature { get; set; } = 1f;

    [JsonPropertyName("minMoisture")]
    public float MinMoisture { get; set; }

    [JsonPropertyName("maxMoisture")]
    public float MaxMoisture { get; set; } = 1f;

    [JsonPropertyName("positionJitter")]
    public Vector3 PositionJitter { get; set; } = Vector3.Zero;

    [JsonPropertyName("rotationAmplitude")]
    public Vector3 RotationAmplitude { get; set; } = Vector3.Zero;

    [JsonPropertyName("positionAmplitude")]
    public Vector3 PositionAmplitude { get; set; } = Vector3.Zero;

    [JsonPropertyName("motionSpeed")]
    public float MotionSpeed { get; set; }
}
