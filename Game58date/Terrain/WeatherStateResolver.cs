#nullable enable
using System;
using Game58date.Gameplay;
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class WeatherStateResolver
{
    public WeatherRuntimeState Resolve(
        WorldSample sample,
        int worldX,
        int worldZ,
        int seed,
        float worldTimeSeconds,
        WorldLawRuntimeState? worldLawState)
    {
        float atmosphere = worldLawState?.World.Atmosphere ?? 0f;
        float pathVisibility = worldLawState?.World.PathVisibility ?? 0f;
        int weatherTick = (int)MathF.Floor(worldTimeSeconds / 18f);
        float localCycle = Hash01(worldX / 8, worldZ / 8, weatherTick + seed);
        float signedCycle = localCycle * 2f - 1f;
        float humidity = sample.Moisture * 0.70f + sample.ShoreWeight * 0.12f + atmosphere * 0.28f + signedCycle * 0.10f;

        WeatherKind targetWeather;
        if ((sample.Biome == BiomeKind.Alpine || sample.Biome == BiomeKind.Mountains) && sample.Temperature < 0.48f && humidity > 0.56f)
        {
            targetWeather = WeatherKind.Snow;
        }
        else if ((sample.Biome == BiomeKind.Shore || sample.Biome == BiomeKind.Wetland) && humidity > 0.54f && atmosphere > 0.16f)
        {
            targetWeather = WeatherKind.Fog;
        }
        else if (humidity > 0.72f)
        {
            targetWeather = WeatherKind.Rain;
        }
        else if (humidity > 0.56f && signedCycle < -0.18f)
        {
            targetWeather = WeatherKind.Fog;
        }
        else if ((sample.Biome == BiomeKind.Hills || sample.Biome == BiomeKind.Scree || sample.Slope > 0.34f) && signedCycle > 0.24f)
        {
            targetWeather = WeatherKind.Wind;
        }
        else
        {
            targetWeather = WeatherKind.Clear;
        }

        float dayPhase = 0.5f + MathF.Sin(worldTimeSeconds * 0.06f) * 0.5f;
        float intensity = targetWeather switch
        {
            WeatherKind.Rain => MathUtil.Clamp(0.55f + humidity * 0.45f, 0f, 1f),
            WeatherKind.Snow => MathUtil.Clamp(0.42f + (0.5f - sample.Temperature) * 0.8f + humidity * 0.2f, 0f, 1f),
            WeatherKind.Fog => MathUtil.Clamp(0.35f + humidity * 0.35f + atmosphere * 0.30f, 0f, 1f),
            WeatherKind.Wind => MathUtil.Clamp(0.20f + sample.Slope * 0.45f + MathF.Max(0f, signedCycle) * 0.35f, 0f, 1f),
            _ => MathUtil.Clamp(0.08f + pathVisibility * 0.10f, 0f, 1f),
        };

        float windAngle = Hash01(worldX / 16, worldZ / 16, seed * 17 + weatherTick * 13) * MathF.PI * 2f;
        Vector2 windDirection = new(MathF.Cos(windAngle), MathF.Sin(windAngle));
        float windStrength = targetWeather == WeatherKind.Wind
            ? MathUtil.Clamp(0.45f + sample.Slope * 0.35f, 0f, 1f)
            : MathUtil.Clamp(0.12f + MathF.Abs(signedCycle) * 0.18f, 0f, 0.45f);

        return new WeatherRuntimeState
        {
            CurrentWeather = targetWeather,
            TargetWeather = targetWeather,
            Blend = 1f,
            Intensity = intensity,
            FogHeight = MathUtil.Clamp(0.7f + humidity * 1.2f + (1f - dayPhase) * 0.35f, 0.6f, 2.2f),
            SnowCoverage = targetWeather == WeatherKind.Snow
                ? MathUtil.Clamp(0.35f + (0.5f - sample.Temperature) * 0.9f + humidity * 0.15f, 0f, 1f)
                : 0f,
            WindDirection = windDirection,
            WindStrength = windStrength,
        };
    }

    private static float Hash01(int x, int z, int seed)
    {
        int hash = x * 374761393 + z * 668265263 + seed * 1442695041;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash ^= hash >> 16;
        return (hash & 1023) / 1023f;
    }
}
