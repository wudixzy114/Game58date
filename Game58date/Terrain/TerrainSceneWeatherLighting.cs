#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Game58date.Terrain;

public static class TerrainSceneWeatherLighting
{
    public static void Apply(LightComponent lightComponent, Color3 baseColor, float baseIntensity, WeatherRuntimeState weatherState, float worldTimeSeconds)
    {
        float dayPhase = 0.5f + MathF.Sin(worldTimeSeconds * 0.06f) * 0.5f;
        float dayWave = 0.46f + dayPhase * 0.82f;
        float weatherDimming = weatherState.TargetWeather switch
        {
            WeatherKind.Rain => 0.16f + weatherState.Intensity * 0.24f,
            WeatherKind.Fog => 0.12f + weatherState.Intensity * 0.20f,
            WeatherKind.Snow => 0.08f + weatherState.Intensity * 0.14f,
            WeatherKind.Wind => 0.04f + weatherState.Intensity * 0.08f,
            _ => 0f,
        };
        float anomalyDimming = weatherState.AnomalyFactor * 0.12f;

        float intensity = MathF.Max(4f, baseIntensity * dayWave * (1f - weatherDimming - anomalyDimming));
        Color3 timeTint = Lerp(
            new Color3(0.58f, 0.60f, 0.78f),
            new Color3(1.00f, 0.97f, 0.92f),
            dayPhase);
        Color3 weatherTint = weatherState.TargetWeather switch
        {
            WeatherKind.Rain => new Color3(0.74f, 0.82f, 0.92f),
            WeatherKind.Fog => weatherState.WoodlandMist > weatherState.SeaFog
                ? new Color3(0.72f, 0.82f, 0.76f)
                : new Color3(0.84f, 0.88f, 0.90f),
            WeatherKind.Snow => new Color3(0.92f, 0.96f, 1.00f),
            WeatherKind.Wind => new Color3(0.96f, 0.90f, 0.80f),
            _ => timeTint,
        };

        Color3 baseTimeColor = Lerp(baseColor, timeTint, 0.34f);
        float blend = MathUtil.Clamp(weatherState.Intensity * 0.56f + weatherState.AnomalyFactor * 0.18f, 0f, 1f);
        lightComponent.Intensity = intensity;
        lightComponent.SetColor(Lerp(baseTimeColor, weatherTint, blend));
    }

    private static Color3 Lerp(Color3 from, Color3 to, float amount)
    {
        amount = MathUtil.Clamp(amount, 0f, 1f);
        return new Color3(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount);
    }
}
