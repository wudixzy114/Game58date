#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class WeatherRuntimeState
{
    public WeatherKind CurrentWeather { get; set; } = WeatherKind.Clear;

    public WeatherKind TargetWeather { get; set; } = WeatherKind.Clear;

    public float Blend { get; set; }

    public float Intensity { get; set; }

    public float FogHeight { get; set; } = 1.0f;

    public float FogDensity { get; set; }

    public Color3 FogColor { get; set; } = new(0.84f, 0.88f, 0.92f);

    public float SnowCoverage { get; set; }

    public float GroundWetness { get; set; }

    public float SeaFog { get; set; }

    public float WoodlandMist { get; set; }

    public float AnomalyFactor { get; set; }

    public Vector2 WindDirection { get; set; } = new Vector2(1f, 0f);

    public float WindStrength { get; set; }
}
