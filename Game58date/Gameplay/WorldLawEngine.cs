#nullable enable
using System;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay;

public sealed class WorldLawEngine
{
    private const int MaxCausalityRecords = 24;

    public WorldLawEngine(WorldLawRuntimeState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        if (State.RuntimeRandomSeed == 0)
        {
            State.RuntimeRandomSeed = 5800;
        }
    }

    public WorldLawRuntimeState State { get; }

    public event Action<string>? LogGenerated;

    public event Action<OmenType, string>? OmenTriggered;

    public event Action<HeroJourneyStage>? HeroStageAdvanced;

    public void Tick(float deltaTimeSeconds)
    {
        if (deltaTimeSeconds <= 0f)
        {
            return;
        }

        State.WorldTimeSeconds += deltaTimeSeconds;
        State.OmenCooldownSeconds = MathF.Max(0f, State.OmenCooldownSeconds - deltaTimeSeconds);
        State.OmenVisualSeconds = MathF.Max(0f, State.OmenVisualSeconds - deltaTimeSeconds);

        WorldLawState world = State.World;
        world.ExplorationDrive = Clamp01(world.ExplorationDrive + deltaTimeSeconds * 0.01f);
        world.BorderLonging = Clamp01(world.BorderLonging - deltaTimeSeconds * 0.01f);
        world.LossMemory = Clamp01(world.LossMemory - deltaTimeSeconds * 0.015f);
        world.BlessingWeight = Clamp01(world.BlessingWeight - deltaTimeSeconds * 0.020f);
        world.ResourcePressure = Clamp01(world.ResourcePressure + MathF.Max(0f, world.Violence - 0.40f) * deltaTimeSeconds * 0.02f);

        if (State.OmenCooldownSeconds <= 0f)
        {
            TrySpawnEmergentOmen();
        }
    }

    public void RegisterExplorationProgress(float distanceMeters)
    {
        if (distanceMeters <= 0f)
        {
            return;
        }

        float normalizedGain = MathF.Min(0.25f, distanceMeters / 240f);
        WorldLawState world = State.World;
        PlayerBehaviorProfile behavior = State.Behavior;

        world.ExplorationDrive = Clamp01(world.ExplorationDrive + normalizedGain * 0.10f);
        world.BorderLonging = Clamp01(world.BorderLonging + normalizedGain * 0.08f);

        behavior.ExplorationTendency = Clamp01(behavior.ExplorationTendency + normalizedGain * 0.06f);

        AddCausalityRecord(PlayerActionKind.ExplorationProgress, $"Exploration progress +{distanceMeters:0.0}m.", 0f, 0f, OmenType.None);
        TryAdvanceHeroStage();
    }

    public void SubmitIntent(string rawIntent)
    {
        string intent = rawIntent.Trim();
        if (string.IsNullOrWhiteSpace(intent))
        {
            EmitLog("Empty intent ignored.");
            return;
        }

        WorldLawState world = State.World;
        PlayerBehaviorProfile behavior = State.Behavior;

        world.IntentCount++;
        world.ExplorationDrive = Clamp01(world.ExplorationDrive + 0.20f);
        behavior.IntentActions++;
        behavior.ExplorationTendency = Clamp01(behavior.ExplorationTendency + 0.10f);
        behavior.CuriosityTendency = Clamp01(behavior.CuriosityTendency + 0.06f);

        string normalized = intent.ToLowerInvariant();
        EmitLog($"Intent: {intent}");

        OmenType resultingOmen;
        string omenDescription;
        float karmaDelta = 0f;
        float blessingDelta = 0f;

        if (ContainsAny(normalized, "sea", "cross", "shore", "ocean", "ship", "harbor"))
        {
            world.BorderLonging = Clamp01(world.BorderLonging + 0.45f);
            world.Curiosity = Clamp01(world.Curiosity + 0.24f);
            world.TargetBiome = "Desert Crossing";
            resultingOmen = OmenType.PathRevelation;
            omenDescription = "Mist opened a route toward a hidden dock.";
        }
        else if (ContainsAny(normalized, "mentor", "guide", "teacher", "sage", "sign"))
        {
            world.Curiosity = Clamp01(world.Curiosity + 0.16f);
            world.Faith = Clamp01(world.Faith + 0.28f);
            behavior.FaithTendency = Clamp01(behavior.FaithTendency + 0.10f);
            resultingOmen = OmenType.GuideArrival;
            omenDescription = "A quiet traveler appeared near the road and pointed north.";
        }
        else if (ContainsAny(normalized, "treasure", "relic", "knowledge", "ruin", "secret"))
        {
            world.Curiosity = Clamp01(world.Curiosity + 0.30f);
            world.ResourcePressure = Clamp01(world.ResourcePressure + 0.12f);
            resultingOmen = OmenType.Divination;
            omenDescription = "Fire sparks drew the outline of a buried geometry.";
        }
        else if (ContainsAny(normalized, "peace", "help", "save", "heal", "kind"))
        {
            RegisterPeacefulChoice("Intent reinforced compassion.");
            resultingOmen = OmenType.SocialShift;
            omenDescription = "The market lights returned and strangers resumed trading news.";
            karmaDelta = 0.16f;
        }
        else if (ContainsAny(normalized, "kill", "take", "destroy", "rule", "loot"))
        {
            RegisterViolentChoice("Intent reinforced violence.");
            resultingOmen = OmenType.NaturalAnomaly;
            omenDescription = "Wind direction reversed and a cold pressure spread across the ground.";
            karmaDelta = -0.18f;
        }
        else
        {
            world.Curiosity = Clamp01(world.Curiosity + 0.12f);
            resultingOmen = OmenType.Divination;
            omenDescription = "The world answered with a brief and uncertain glow.";
        }

        AddCausalityRecord(PlayerActionKind.IntentSubmitted, $"Intent submitted: {intent}", karmaDelta, blessingDelta, resultingOmen);
        TriggerOmen(resultingOmen, omenDescription);
        TryAdvanceHeroStage();
    }

    public void RegisterLoss(string reason)
    {
        WorldLawState world = State.World;
        PlayerBehaviorProfile behavior = State.Behavior;

        world.LossMemory = Clamp01(world.LossMemory + 0.50f);
        world.BlessingWeight = Clamp01(world.BlessingWeight + 0.42f);
        behavior.LossEvents++;

        EmitLog(reason);
        AddCausalityRecord(PlayerActionKind.LossRegistered, reason, 0f, +0.42f, OmenType.Divination);
        TriggerOmen(OmenType.Divination, "A loss shifted fortune back toward the player.");
    }

    public void RegisterViolentChoice(string reason = "Behavior profile moved toward violence.")
    {
        WorldLawState world = State.World;
        PlayerBehaviorProfile behavior = State.Behavior;

        world.Karma = MathF.Max(-1f, world.Karma - 0.18f);
        world.Violence = Clamp01(world.Violence + 0.24f);
        world.ResourcePressure = Clamp01(world.ResourcePressure + 0.10f);

        behavior.ViolentActions++;
        behavior.ViolentTendency = Clamp01(behavior.ViolentTendency + 0.16f);
        behavior.PeacefulTendency = Clamp01(behavior.PeacefulTendency - 0.05f);

        EmitLog(reason);
        AddCausalityRecord(PlayerActionKind.ViolentChoice, reason, -0.18f, 0f, OmenType.None);
        TryAdvanceHeroStage();
    }

    public void RegisterPeacefulChoice(string reason = "Behavior profile moved toward compassion.")
    {
        WorldLawState world = State.World;
        PlayerBehaviorProfile behavior = State.Behavior;

        world.Karma = MathF.Min(1f, world.Karma + 0.16f);
        world.Faith = Clamp01(world.Faith + 0.12f);
        world.Violence = Clamp01(world.Violence - 0.08f);

        behavior.PeacefulActions++;
        behavior.PeacefulTendency = Clamp01(behavior.PeacefulTendency + 0.14f);
        behavior.FaithTendency = Clamp01(behavior.FaithTendency + 0.08f);
        behavior.ViolentTendency = Clamp01(behavior.ViolentTendency - 0.04f);

        EmitLog(reason);
        AddCausalityRecord(PlayerActionKind.PeacefulChoice, reason, +0.16f, 0f, OmenType.None);
        TryAdvanceHeroStage();
    }

    private void TrySpawnEmergentOmen()
    {
        WorldLawState world = State.World;
        if (world.ResourcePressure > 0.72f)
        {
            TriggerOmen(OmenType.NaturalAnomaly, "Resource pressure triggered a backlash.");
            world.ResourcePressure = Clamp01(world.ResourcePressure - 0.25f);
            return;
        }

        if (world.BlessingWeight > 0.65f)
        {
            TriggerOmen(OmenType.PathRevelation, "A hidden route appeared after a loss.");
            world.BlessingWeight = Clamp01(world.BlessingWeight - 0.30f);
            return;
        }

        if (world.Karma < -0.55f)
        {
            TriggerOmen(OmenType.NaturalAnomaly, "Bad karma turned even clear weather hostile.");
            world.Karma = MathF.Min(1f, world.Karma + 0.12f);
            return;
        }

        if (world.Karma > 0.55f && NextRandomFloat() > 0.45f)
        {
            TriggerOmen(OmenType.GuideArrival, "A patient light waited longer than before.");
            return;
        }

        if (world.ExplorationDrive > 0.68f && NextRandomFloat() > 0.55f)
        {
            TriggerOmen(OmenType.SocialShift, "A distant harbor bell suggested long-range travel was possible.");
        }
    }

    private void TriggerOmen(OmenType omenType, string description)
    {
        WorldLawState world = State.World;
        State.OmenCooldownSeconds = 8f + NextRandomFloat() * 4f;
        State.OmenVisualSeconds = 6f;
        world.LastOmen = omenType;
        world.LastOmenDescription = description;
        world.OmenCount++;

        switch (omenType)
        {
            case OmenType.PathRevelation:
                world.PathVisibility = Clamp01(world.PathVisibility + 0.55f);
                world.BorderLonging = Clamp01(world.BorderLonging - 0.22f);
                break;
            case OmenType.GuideArrival:
                world.Faith = Clamp01(world.Faith + 0.18f);
                break;
            case OmenType.NaturalAnomaly:
                world.Atmosphere = Clamp01(world.Atmosphere + 0.22f);
                break;
            case OmenType.SocialShift:
                world.SocialFlux = Clamp01(world.SocialFlux + 0.24f);
                break;
            case OmenType.Divination:
                world.Curiosity = Clamp01(world.Curiosity + 0.18f);
                break;
        }

        EmitLog($"Omen: {description}");
        OmenTriggered?.Invoke(omenType, description);
        TryAdvanceHeroStage();
    }

    private void TryAdvanceHeroStage()
    {
        WorldLawState world = State.World;
        HeroJourneyStage nextStage = State.HeroStage;

        if (world.IntentCount >= 1 && world.ExplorationDrive > 0.12f)
        {
            nextStage = HeroJourneyStage.CallToAdventure;
        }

        if (world.OmenCount >= 1 && world.Curiosity > 0.26f)
        {
            nextStage = HeroJourneyStage.CrossingTheThreshold;
        }

        if (world.BorderLonging > 0.40f || world.PathVisibility > 0.45f)
        {
            nextStage = HeroJourneyStage.RoadOfTrials;
        }

        if (world.Faith > 0.45f && world.Karma > 0.10f)
        {
            nextStage = HeroJourneyStage.MeetingTheMentor;
        }

        if (world.PathVisibility > 0.70f && world.Curiosity > 0.55f)
        {
            nextStage = HeroJourneyStage.ApproachToTheInmostCave;
        }

        if (world.BlessingWeight > 0.55f && world.OmenCount >= 3)
        {
            nextStage = HeroJourneyStage.Transformation;
        }

        if (nextStage <= State.HeroStage)
        {
            return;
        }

        State.HeroStage = nextStage;
        EmitLog($"Hero stage advanced to {GetStageTitle(nextStage)}.");
        HeroStageAdvanced?.Invoke(nextStage);
    }

    private void AddCausalityRecord(PlayerActionKind actionKind, string summary, float karmaDelta, float blessingDelta, OmenType omenType)
    {
        PlayerBehaviorProfile behavior = State.Behavior;
        behavior.RecordedActions++;

        State.RecentCausality.Add(new CausalityRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            ActionKind = actionKind,
            Summary = summary,
            KarmaDelta = karmaDelta,
            BlessingDelta = blessingDelta,
            TriggeredOmen = omenType,
        });

        while (State.RecentCausality.Count > MaxCausalityRecords)
        {
            State.RecentCausality.RemoveAt(0);
        }
    }

    private void EmitLog(string message)
    {
        LogGenerated?.Invoke(message);
    }

    private float NextRandomFloat()
    {
        uint next = unchecked((uint)State.RuntimeRandomSeed * 1664525u + 1013904223u);
        State.RuntimeRandomSeed = (int)next;
        return (next & 0x00FFFFFFu) / 16777216f;
    }

    private static float Clamp01(float value)
    {
        return MathUtil.Clamp(value, 0f, 1f);
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetStageTitle(HeroJourneyStage stage)
    {
        return stage switch
        {
            HeroJourneyStage.OrdinaryWorld => "Ordinary World",
            HeroJourneyStage.CallToAdventure => "Call To Adventure",
            HeroJourneyStage.CrossingTheThreshold => "Crossing The Threshold",
            HeroJourneyStage.RoadOfTrials => "Road Of Trials",
            HeroJourneyStage.MeetingTheMentor => "Meeting The Mentor",
            HeroJourneyStage.ApproachToTheInmostCave => "Approach To The Inmost Cave",
            HeroJourneyStage.Transformation => "Transformation",
            _ => "Unknown",
        };
    }

    public static string GetOmenTitle(OmenType omen)
    {
        return omen switch
        {
            OmenType.NaturalAnomaly => "Natural Anomaly",
            OmenType.SocialShift => "Social Shift",
            OmenType.GuideArrival => "Guide Arrival",
            OmenType.Divination => "Divination",
            OmenType.PathRevelation => "Path Revelation",
            _ => "None",
        };
    }
}
