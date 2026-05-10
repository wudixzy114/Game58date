#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Game58date.Gameplay.UI;

public static class GameUiSceneLighting
{
    public static void Apply(LightComponent lightComponent, Color3 baseLightColor, float baseLightIntensity, WorldLawRuntimeState state)
    {
        if (lightComponent is null)
        {
            return;
        }

        float worldTime = state.WorldTimeSeconds;
        float baseWave = 0.88f + MathF.Sin(worldTime * 0.8f) * 0.12f;
        OmenType activeOmenType = state.Omen.ActiveOmen?.OmenType ?? state.World.LastOmen;
        float omenBoost = activeOmenType switch
        {
            OmenType.PathRevelation => 4.5f,
            OmenType.GuideArrival => 2.2f,
            OmenType.NaturalAnomaly => -4.0f,
            OmenType.SocialShift => 1.2f,
            OmenType.Divination => 3.0f,
            _ => 0f,
        };

        float perceptionBoost = state.Perception.IsActive ? state.Perception.Intensity * 4.5f : 0f;
        lightComponent.Intensity = MathF.Max(5f, baseLightIntensity * baseWave + omenBoost + perceptionBoost + state.World.Karma * 1.4f);

        Color3 stageColor = state.Narrative.CurrentStage switch
        {
            HeroJourneyStage.OrdinaryWorld => new Color3(1.00f, 1.00f, 1.00f),
            HeroJourneyStage.CallToAdventure => new Color3(1.00f, 0.92f, 0.80f),
            HeroJourneyStage.CrossingTheThreshold => new Color3(0.88f, 0.95f, 1.00f),
            HeroJourneyStage.RoadOfTrials => new Color3(1.00f, 0.80f, 0.55f),
            HeroJourneyStage.MeetingTheMentor => new Color3(0.82f, 1.00f, 0.84f),
            HeroJourneyStage.ApproachToTheInmostCave => new Color3(0.75f, 0.82f, 1.00f),
            HeroJourneyStage.Transformation => new Color3(1.00f, 0.96f, 0.66f),
            _ => baseLightColor,
        };

        float omenBlendBoost = state.Omen.ActiveOmen is null ? 0f : state.Omen.ActiveOmen.Score * 0.15f;
        omenBlendBoost += state.Perception.IsActive ? state.Perception.Intensity * 0.18f : 0f;
        float blend = MathUtil.Clamp(state.World.Atmosphere * 0.55f + state.World.PathVisibility * 0.45f + omenBlendBoost, 0f, 1f);
        lightComponent.SetColor(LerpColor(baseLightColor, stageColor, blend));
    }

    private static Color3 LerpColor(Color3 from, Color3 to, float amount)
    {
        amount = MathUtil.Clamp(amount, 0f, 1f);
        return new Color3(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount);
    }
}
