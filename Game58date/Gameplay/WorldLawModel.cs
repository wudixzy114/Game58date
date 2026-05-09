#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game58date.Gameplay;

public enum OmenType
{
    None = 0,
    NaturalAnomaly = 1,
    SocialShift = 2,
    GuideArrival = 3,
    Divination = 4,
    PathRevelation = 5,
}

public enum HeroJourneyStage
{
    OrdinaryWorld = 0,
    CallToAdventure = 1,
    CrossingTheThreshold = 2,
    RoadOfTrials = 3,
    MeetingTheMentor = 4,
    ApproachToTheInmostCave = 5,
    Transformation = 6,
}

public enum PlayerActionKind
{
    None = 0,
    IntentSubmitted = 1,
    PeacefulChoice = 2,
    ViolentChoice = 3,
    LossRegistered = 4,
    ExplorationProgress = 5,
}

public sealed class WorldLawState
{
    public float ExplorationDrive { get; set; } = 0.08f;

    public float BorderLonging { get; set; } = 0.05f;

    public float BlessingWeight { get; set; } = 0.10f;

    public float ResourcePressure { get; set; } = 0.10f;

    public float Karma { get; set; }

    public float Violence { get; set; } = 0.10f;

    public float Faith { get; set; } = 0.08f;

    public float Curiosity { get; set; } = 0.12f;

    public float PathVisibility { get; set; }

    public float SocialFlux { get; set; } = 0.05f;

    public float Atmosphere { get; set; } = 0.10f;

    public float LossMemory { get; set; }

    public int IntentCount { get; set; }

    public int OmenCount { get; set; }

    public OmenType LastOmen { get; set; }

    public string LastOmenDescription { get; set; } = "No omen yet.";

    public string TargetBiome { get; set; } = "Morning Plain";
}

public sealed class PlayerBehaviorProfile
{
    public float PeacefulTendency { get; set; } = 0.10f;

    public float ViolentTendency { get; set; } = 0.10f;

    public float ExplorationTendency { get; set; } = 0.10f;

    public float FaithTendency { get; set; } = 0.08f;

    public float CuriosityTendency { get; set; } = 0.12f;

    public int RecordedActions { get; set; }

    public int IntentActions { get; set; }

    public int PeacefulActions { get; set; }

    public int ViolentActions { get; set; }

    public int LossEvents { get; set; }
}

public sealed class CausalityRecord
{
    public DateTimeOffset TimestampUtc { get; set; }

    public PlayerActionKind ActionKind { get; set; }

    public string Summary { get; set; } = string.Empty;

    public float KarmaDelta { get; set; }

    public float BlessingDelta { get; set; }

    public OmenType TriggeredOmen { get; set; }
}

public sealed class WorldLawRuntimeState
{
    public WorldLawState World { get; set; } = new();

    public PlayerBehaviorProfile Behavior { get; set; } = new();

    public HeroJourneyStage HeroStage { get; set; } = HeroJourneyStage.OrdinaryWorld;

    public float OmenCooldownSeconds { get; set; }

    public float OmenVisualSeconds { get; set; }

    public float WorldTimeSeconds { get; set; }

    public int RuntimeRandomSeed { get; set; } = 5800;

    public List<CausalityRecord> RecentCausality { get; set; } = new();

    public PlayerIntentRuntimeState Intent { get; set; } = new();

    public HeroJourneyRuntimeState Narrative { get; set; } = new();

    public WorldLawRuntimeState Clone()
    {
        return new WorldLawRuntimeState
        {
            World = new WorldLawState
            {
                ExplorationDrive = World.ExplorationDrive,
                BorderLonging = World.BorderLonging,
                BlessingWeight = World.BlessingWeight,
                ResourcePressure = World.ResourcePressure,
                Karma = World.Karma,
                Violence = World.Violence,
                Faith = World.Faith,
                Curiosity = World.Curiosity,
                PathVisibility = World.PathVisibility,
                SocialFlux = World.SocialFlux,
                Atmosphere = World.Atmosphere,
                LossMemory = World.LossMemory,
                IntentCount = World.IntentCount,
                OmenCount = World.OmenCount,
                LastOmen = World.LastOmen,
                LastOmenDescription = World.LastOmenDescription,
                TargetBiome = World.TargetBiome,
            },
            Behavior = new PlayerBehaviorProfile
            {
                PeacefulTendency = Behavior.PeacefulTendency,
                ViolentTendency = Behavior.ViolentTendency,
                ExplorationTendency = Behavior.ExplorationTendency,
                FaithTendency = Behavior.FaithTendency,
                CuriosityTendency = Behavior.CuriosityTendency,
                RecordedActions = Behavior.RecordedActions,
                IntentActions = Behavior.IntentActions,
                PeacefulActions = Behavior.PeacefulActions,
                ViolentActions = Behavior.ViolentActions,
                LossEvents = Behavior.LossEvents,
            },
            HeroStage = HeroStage,
            OmenCooldownSeconds = OmenCooldownSeconds,
            OmenVisualSeconds = OmenVisualSeconds,
            WorldTimeSeconds = WorldTimeSeconds,
            RuntimeRandomSeed = RuntimeRandomSeed,
            RecentCausality = RecentCausality
                .Select(record => new CausalityRecord
                {
                    TimestampUtc = record.TimestampUtc,
                    ActionKind = record.ActionKind,
                    Summary = record.Summary,
                    KarmaDelta = record.KarmaDelta,
                    BlessingDelta = record.BlessingDelta,
                    TriggeredOmen = record.TriggeredOmen,
                })
                .ToList(),
            Intent = Intent.Clone(),
            Narrative = Narrative.Clone(),
        };
    }
}

public enum IntentTopic
{
    Unknown = 0,
    Exploration = 1,
    Mentor = 2,
    Knowledge = 3,
    Compassion = 4,
    Domination = 5,
}

public sealed class PlayerIntentRecord
{
    public DateTimeOffset TimestampUtc { get; set; }

    public string RawText { get; set; } = string.Empty;

    public IntentTopic Topic { get; set; }

    public float Confidence { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string SuggestedTargetBiome { get; set; } = string.Empty;
}

public sealed class PlayerIntentRuntimeState
{
    public bool TextInputEnabled { get; set; } = true;

    public int SubmittedIntentCount { get; set; }

    public PlayerIntentRecord? LastIntent { get; set; }

    public List<PlayerIntentRecord> RecentIntents { get; set; } = new();

    public PlayerIntentRuntimeState Clone()
    {
        return new PlayerIntentRuntimeState
        {
            TextInputEnabled = TextInputEnabled,
            SubmittedIntentCount = SubmittedIntentCount,
            LastIntent = LastIntent is null
                ? null
                : new PlayerIntentRecord
                {
                    TimestampUtc = LastIntent.TimestampUtc,
                    RawText = LastIntent.RawText,
                    Topic = LastIntent.Topic,
                    Confidence = LastIntent.Confidence,
                    Summary = LastIntent.Summary,
                    SuggestedTargetBiome = LastIntent.SuggestedTargetBiome,
                },
            RecentIntents = RecentIntents
                .Select(intent => new PlayerIntentRecord
                {
                    TimestampUtc = intent.TimestampUtc,
                    RawText = intent.RawText,
                    Topic = intent.Topic,
                    Confidence = intent.Confidence,
                    Summary = intent.Summary,
                    SuggestedTargetBiome = intent.SuggestedTargetBiome,
                })
                .ToList(),
        };
    }
}

public sealed class HeroJourneyRuntimeState
{
    public HeroJourneyStage CurrentStage { get; set; } = HeroJourneyStage.OrdinaryWorld;

    public string LastStageReason { get; set; } = "Initial state.";

    public DateTimeOffset? LastAdvancedUtc { get; set; }

    public List<HeroJourneyStageRecord> StageHistory { get; set; } = new();

    public HeroJourneyRuntimeState Clone()
    {
        return new HeroJourneyRuntimeState
        {
            CurrentStage = CurrentStage,
            LastStageReason = LastStageReason,
            LastAdvancedUtc = LastAdvancedUtc,
            StageHistory = StageHistory
                .Select(entry => new HeroJourneyStageRecord
                {
                    Stage = entry.Stage,
                    TimestampUtc = entry.TimestampUtc,
                    Reason = entry.Reason,
                })
                .ToList(),
        };
    }
}

public sealed class HeroJourneyStageRecord
{
    public HeroJourneyStage Stage { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public string Reason { get; set; } = string.Empty;
}
