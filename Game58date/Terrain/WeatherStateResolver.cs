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
        float perception = worldLawState?.Perception.IsActive == true
            ? worldLawState.Perception.Intensity
            : 0f;
        float anomaly = worldLawState?.Omen.ActiveOmen?.OmenType == OmenType.NaturalAnomaly
            ? worldLawState.Omen.ActiveOmen.Score
            : 0f;
        float guideInfluence = worldLawState?.Omen.ActiveOmen?.OmenType is OmenType.PathRevelation or OmenType.GuideArrival
            ? worldLawState.Omen.ActiveOmen.Score
            : 0f;

        float coastal = MathF.Max(sample.Weights.Shore, sample.ShoreWeight);
        float wetland = MathF.Max(sample.Weights.Wetland, sample.WetlandWeight);
        float woodland = MathF.Max(sample.Weights.Woodland, sample.WoodlandWeight);
        float alpine = MathF.Max(sample.Weights.Alpine, sample.AlpineWeight);
        float scree = MathF.Max(sample.Weights.Scree, sample.ScreeWeight);
        float terrainExposure = MathF.Max(sample.Slope, sample.TreeLine);
        int weatherTick = (int)MathF.Floor(worldTimeSeconds / 18f);
        float localCycle = Hash01(worldX / 8, worldZ / 8, weatherTick + seed);
        float signedCycle = localCycle * 2f - 1f;
        float dayPhase = 0.5f + MathF.Sin(worldTimeSeconds * 0.06f) * 0.5f;
        float dawnDusk = 1f - MathF.Abs(dayPhase - 0.5f) * 2f;
        float night = 1f - dayPhase;
        float humidity = MathUtil.Clamp(
            sample.Moisture * 0.56f +
            coastal * 0.16f +
            wetland * 0.16f +
            atmosphere * 0.16f +
            anomaly * 0.12f +
            signedCycle * 0.08f -
            terrainExposure * 0.04f,
            0f,
            1.1f);
        float seaFogPotential = MathUtil.Clamp(coastal * 0.62f + humidity * 0.28f + night * 0.10f + anomaly * 0.06f, 0f, 1f);
        float woodlandMist = MathUtil.Clamp(woodland * 0.58f + humidity * 0.22f + dawnDusk * 0.18f + night * 0.08f - sample.TreeLine * 0.14f, 0f, 1f);
        float snowPotential = MathUtil.Clamp(alpine * 0.48f + sample.SnowCoverMask * 0.34f + (0.54f - sample.Temperature) * 0.58f + humidity * 0.10f, 0f, 1f);
        float rainPotential = MathUtil.Clamp(humidity * 0.72f + atmosphere * 0.12f + anomaly * 0.12f - snowPotential * 0.35f, 0f, 1f);
        float windPotential = MathUtil.Clamp(scree * 0.44f + terrainExposure * 0.38f + MathF.Max(0f, signedCycle) * 0.18f + anomaly * 0.18f, 0f, 1f);

        WeatherKind targetWeather;
        if ((sample.Biome == BiomeKind.Alpine || sample.Biome == BiomeKind.Mountains) && snowPotential > 0.52f)
        {
            targetWeather = WeatherKind.Snow;
        }
        else if (seaFogPotential > 0.56f || woodlandMist > 0.64f)
        {
            targetWeather = WeatherKind.Fog;
        }
        else if (rainPotential > 0.68f)
        {
            targetWeather = WeatherKind.Rain;
        }
        else if (humidity > 0.52f && signedCycle < -0.18f)
        {
            targetWeather = WeatherKind.Fog;
        }
        else if ((sample.Biome == BiomeKind.Hills || sample.Biome == BiomeKind.Scree || sample.Slope > 0.34f) && windPotential > 0.46f)
        {
            targetWeather = WeatherKind.Wind;
        }
        else
        {
            targetWeather = WeatherKind.Clear;
        }

        float intensity = targetWeather switch
        {
            WeatherKind.Rain => MathUtil.Clamp(0.42f + rainPotential * 0.58f, 0f, 1f),
            WeatherKind.Snow => MathUtil.Clamp(0.38f + snowPotential * 0.62f, 0f, 1f),
            WeatherKind.Fog => MathUtil.Clamp(0.28f + MathF.Max(seaFogPotential, woodlandMist) * 0.62f, 0f, 1f),
            WeatherKind.Wind => MathUtil.Clamp(0.22f + windPotential * 0.62f, 0f, 1f),
            _ => MathUtil.Clamp(0.06f + pathVisibility * 0.08f + guideInfluence * 0.06f, 0f, 1f),
        };

        float windAngle = Hash01(worldX / 16, worldZ / 16, seed * 17 + weatherTick * 13) * MathF.PI * 2f;
        Vector2 windDirection = new(MathF.Cos(windAngle), MathF.Sin(windAngle));
        float windStrength = targetWeather == WeatherKind.Wind
            ? MathUtil.Clamp(0.38f + windPotential * 0.54f, 0f, 1f)
            : MathUtil.Clamp(0.10f + MathF.Abs(signedCycle) * 0.12f + anomaly * 0.12f, 0f, 0.45f);
        float fogDensity = MathUtil.Clamp(
            0.05f +
            seaFogPotential * 0.22f +
            woodlandMist * 0.20f +
            (targetWeather == WeatherKind.Fog ? intensity * 0.18f : 0f) +
            (targetWeather == WeatherKind.Rain ? intensity * 0.08f : 0f),
            0f,
            0.65f);
        float fogHeight = MathUtil.Clamp(
            0.72f +
            humidity * 0.95f +
            woodlandMist * 0.16f -
            seaFogPotential * 0.24f -
            sample.TreeLine * 0.30f +
            night * 0.18f,
            0.48f,
            2.25f);
        Color3 baseFogColor = Lerp(
            new Color3(0.34f, 0.38f, 0.46f),
            new Color3(0.88f, 0.92f, 0.98f),
            dayPhase);
        Color3 weatherFogTint = targetWeather switch
        {
            WeatherKind.Rain => new Color3(0.58f, 0.66f, 0.76f),
            WeatherKind.Snow => new Color3(0.84f, 0.90f, 0.98f),
            WeatherKind.Fog => seaFogPotential >= woodlandMist
                ? new Color3(0.68f, 0.78f, 0.84f)
                : new Color3(0.60f, 0.70f, 0.64f),
            WeatherKind.Wind => new Color3(0.74f, 0.70f, 0.60f),
            _ => new Color3(0.80f, 0.86f, 0.92f),
        };

        return new WeatherRuntimeState
        {
            CurrentWeather = targetWeather,
            TargetWeather = targetWeather,
            Blend = 1f,
            Intensity = intensity,
            FogHeight = fogHeight,
            FogDensity = fogDensity,
            FogColor = Lerp(baseFogColor, weatherFogTint, MathUtil.Clamp(0.28f + intensity * 0.44f, 0f, 1f)),
            SnowCoverage = targetWeather == WeatherKind.Snow
                ? MathUtil.Clamp(0.22f + snowPotential * 0.78f, 0f, 1f)
                : 0f,
            GroundWetness = MathUtil.Clamp(wetland * 0.46f + rainPotential * 0.50f + seaFogPotential * 0.10f, 0f, 1f),
            SeaFog = seaFogPotential,
            WoodlandMist = woodlandMist,
            AnomalyFactor = MathUtil.Clamp(anomaly + perception * 0.24f, 0f, 1f),
            WindDirection = windDirection,
            WindStrength = windStrength,
        };
    }

    private static Color3 Lerp(Color3 from, Color3 to, float amount)
    {
        amount = MathUtil.Clamp(amount, 0f, 1f);
        return new Color3(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount);
    }

    private static float Hash01(int x, int z, int seed)
    {
        int hash = x * 374761393 + z * 668265263 + seed * 1442695041;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash ^= hash >> 16;
        return (hash & 1023) / 1023f;
    }
}
