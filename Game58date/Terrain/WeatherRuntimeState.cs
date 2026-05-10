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

    public float SnowCoverage { get; set; }

    public Vector2 WindDirection { get; set; } = new Vector2(1f, 0f);

    public float WindStrength { get; set; }
}
