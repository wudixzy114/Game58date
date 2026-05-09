#nullable enable
using System.Collections.Generic;
using System.Linq;
using Game58date.Save;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay;

public static class WorldLawSaveMapper
{
    public static WorldLawRuntimeState BuildRuntimeState(GameplaySaveData? gameplaySaveData)
    {
        gameplaySaveData ??= new GameplaySaveData();
        WorldLawSaveData? saveData = gameplaySaveData.WorldLaw;
        if (saveData is null)
        {
            return new WorldLawRuntimeState();
        }

        var runtimeState = new WorldLawRuntimeState
        {
            HeroStage = saveData.HeroStage,
            OmenCooldownSeconds = saveData.OmenCooldownSeconds,
            OmenVisualSeconds = saveData.OmenVisualSeconds,
            WorldTimeSeconds = saveData.WorldTimeSeconds,
            RuntimeRandomSeed = saveData.RuntimeRandomSeed == 0 ? 5800 : saveData.RuntimeRandomSeed,
            World = new WorldLawState
            {
                ExplorationDrive = saveData.World.ExplorationDrive,
                BorderLonging = saveData.World.BorderLonging,
                BlessingWeight = saveData.World.BlessingWeight,
                ResourcePressure = saveData.World.ResourcePressure,
                Karma = saveData.World.Karma,
                Violence = saveData.World.Violence,
                Faith = saveData.World.Faith,
                Curiosity = saveData.World.Curiosity,
                PathVisibility = saveData.World.PathVisibility,
                SocialFlux = saveData.World.SocialFlux,
                Atmosphere = saveData.World.Atmosphere,
                LossMemory = saveData.World.LossMemory,
                IntentCount = saveData.World.IntentCount,
                OmenCount = saveData.World.OmenCount,
                LastOmen = saveData.World.LastOmen,
                LastOmenDescription = string.IsNullOrWhiteSpace(saveData.World.LastOmenDescription) ? "No omen yet." : saveData.World.LastOmenDescription,
                TargetBiome = string.IsNullOrWhiteSpace(saveData.World.TargetBiome) ? "Morning Plain" : saveData.World.TargetBiome,
            },
            Behavior = new PlayerBehaviorProfile
            {
                PeacefulTendency = saveData.Behavior.PeacefulTendency,
                ViolentTendency = saveData.Behavior.ViolentTendency,
                ExplorationTendency = saveData.Behavior.ExplorationTendency,
                FaithTendency = saveData.Behavior.FaithTendency,
                CuriosityTendency = saveData.Behavior.CuriosityTendency,
                RecordedActions = saveData.Behavior.RecordedActions,
                IntentActions = saveData.Behavior.IntentActions,
                PeacefulActions = saveData.Behavior.PeacefulActions,
                ViolentActions = saveData.Behavior.ViolentActions,
                LossEvents = saveData.Behavior.LossEvents,
            },
            RecentCausality = saveData.RecentCausality
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
            Intent = new PlayerIntentRuntimeState
            {
                TextInputEnabled = gameplaySaveData.Intent.TextInputEnabled,
                SubmittedIntentCount = gameplaySaveData.Intent.SubmittedIntentCount,
                LastIntent = gameplaySaveData.Intent.LastIntent is null
                    ? null
                    : new PlayerIntentRecord
                    {
                        TimestampUtc = gameplaySaveData.Intent.LastIntent.TimestampUtc,
                        RawText = gameplaySaveData.Intent.LastIntent.RawText,
                        Topic = gameplaySaveData.Intent.LastIntent.Topic,
                        Confidence = gameplaySaveData.Intent.LastIntent.Confidence,
                        Summary = gameplaySaveData.Intent.LastIntent.Summary,
                        SuggestedTargetBiome = gameplaySaveData.Intent.LastIntent.SuggestedTargetBiome,
                    },
                RecentIntents = gameplaySaveData.Intent.RecentIntents
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
            },
            Narrative = new HeroJourneyRuntimeState
            {
                CurrentStage = gameplaySaveData.Narrative.CurrentStage,
                LastStageReason = string.IsNullOrWhiteSpace(gameplaySaveData.Narrative.LastStageReason) ? "Initial state." : gameplaySaveData.Narrative.LastStageReason,
                LastAdvancedUtc = gameplaySaveData.Narrative.LastAdvancedUtc,
                StageHistory = gameplaySaveData.Narrative.StageHistory
                    .Select(stage => new HeroJourneyStageRecord
                    {
                        Stage = stage.Stage,
                        TimestampUtc = stage.TimestampUtc,
                        Reason = stage.Reason,
                    })
                    .ToList(),
            },
            Omen = new OmenRuntimeState
            {
                CooldownSeconds = gameplaySaveData.Omen.CooldownSeconds,
                VisualSeconds = gameplaySaveData.Omen.VisualSeconds,
                LastScore = gameplaySaveData.Omen.LastScore,
                LastSource = gameplaySaveData.Omen.LastSource,
                ActiveOmen = gameplaySaveData.Omen.ActiveOmen is null
                    ? null
                    : new OmenRecord
                    {
                        TimestampUtc = gameplaySaveData.Omen.ActiveOmen.TimestampUtc,
                        OmenType = gameplaySaveData.Omen.ActiveOmen.OmenType,
                        Source = gameplaySaveData.Omen.ActiveOmen.Source,
                        Score = gameplaySaveData.Omen.ActiveOmen.Score,
                        Description = gameplaySaveData.Omen.ActiveOmen.Description,
                    },
                History = gameplaySaveData.Omen.History
                    .Select(omen => new OmenRecord
                    {
                        TimestampUtc = omen.TimestampUtc,
                        OmenType = omen.OmenType,
                        Source = omen.Source,
                        Score = omen.Score,
                        Description = omen.Description,
                    })
                    .ToList(),
            },
            Perception = new PerceptionRuntimeState
            {
                IsActive = gameplaySaveData.Perception.IsActive,
                ActiveSecondsRemaining = gameplaySaveData.Perception.ActiveSecondsRemaining,
                CooldownSecondsRemaining = gameplaySaveData.Perception.CooldownSecondsRemaining,
                Intensity = gameplaySaveData.Perception.Intensity,
                ActivationCount = gameplaySaveData.Perception.ActivationCount,
                LastActivatedUtc = gameplaySaveData.Perception.LastActivatedUtc,
            },
        };

        ClampRuntimeState(runtimeState);
        return runtimeState;
    }

    public static GameplaySaveData CreateSaveData(WorldLawRuntimeState runtimeState)
    {
        ClampRuntimeState(runtimeState);

        return new GameplaySaveData
        {
            WorldLaw = new WorldLawSaveData
            {
                HeroStage = runtimeState.HeroStage,
                OmenCooldownSeconds = runtimeState.OmenCooldownSeconds,
                OmenVisualSeconds = runtimeState.OmenVisualSeconds,
                WorldTimeSeconds = runtimeState.WorldTimeSeconds,
                RuntimeRandomSeed = runtimeState.RuntimeRandomSeed,
                World = new WorldLawStateSaveData
                {
                    ExplorationDrive = runtimeState.World.ExplorationDrive,
                    BorderLonging = runtimeState.World.BorderLonging,
                    BlessingWeight = runtimeState.World.BlessingWeight,
                    ResourcePressure = runtimeState.World.ResourcePressure,
                    Karma = runtimeState.World.Karma,
                    Violence = runtimeState.World.Violence,
                    Faith = runtimeState.World.Faith,
                    Curiosity = runtimeState.World.Curiosity,
                    PathVisibility = runtimeState.World.PathVisibility,
                    SocialFlux = runtimeState.World.SocialFlux,
                    Atmosphere = runtimeState.World.Atmosphere,
                    LossMemory = runtimeState.World.LossMemory,
                    IntentCount = runtimeState.World.IntentCount,
                    OmenCount = runtimeState.World.OmenCount,
                    LastOmen = runtimeState.World.LastOmen,
                    LastOmenDescription = runtimeState.World.LastOmenDescription,
                    TargetBiome = runtimeState.World.TargetBiome,
                },
                Behavior = new PlayerBehaviorSaveData
                {
                    PeacefulTendency = runtimeState.Behavior.PeacefulTendency,
                    ViolentTendency = runtimeState.Behavior.ViolentTendency,
                    ExplorationTendency = runtimeState.Behavior.ExplorationTendency,
                    FaithTendency = runtimeState.Behavior.FaithTendency,
                    CuriosityTendency = runtimeState.Behavior.CuriosityTendency,
                    RecordedActions = runtimeState.Behavior.RecordedActions,
                    IntentActions = runtimeState.Behavior.IntentActions,
                    PeacefulActions = runtimeState.Behavior.PeacefulActions,
                    ViolentActions = runtimeState.Behavior.ViolentActions,
                    LossEvents = runtimeState.Behavior.LossEvents,
                },
                RecentCausality = runtimeState.RecentCausality
                    .Select(record => new CausalityRecordSaveData
                    {
                        TimestampUtc = record.TimestampUtc,
                        ActionKind = record.ActionKind,
                        Summary = record.Summary,
                        KarmaDelta = record.KarmaDelta,
                        BlessingDelta = record.BlessingDelta,
                        TriggeredOmen = record.TriggeredOmen,
                    })
                    .ToList(),
            },
            Intent = new PlayerIntentSaveData
            {
                TextInputEnabled = runtimeState.Intent.TextInputEnabled,
                SubmittedIntentCount = runtimeState.Intent.SubmittedIntentCount,
                LastIntent = runtimeState.Intent.LastIntent is null
                    ? null
                    : new PlayerIntentRecordSaveData
                    {
                        TimestampUtc = runtimeState.Intent.LastIntent.TimestampUtc,
                        RawText = runtimeState.Intent.LastIntent.RawText,
                        Topic = runtimeState.Intent.LastIntent.Topic,
                        Confidence = runtimeState.Intent.LastIntent.Confidence,
                        Summary = runtimeState.Intent.LastIntent.Summary,
                        SuggestedTargetBiome = runtimeState.Intent.LastIntent.SuggestedTargetBiome,
                    },
                RecentIntents = runtimeState.Intent.RecentIntents
                    .Select(intent => new PlayerIntentRecordSaveData
                    {
                        TimestampUtc = intent.TimestampUtc,
                        RawText = intent.RawText,
                        Topic = intent.Topic,
                        Confidence = intent.Confidence,
                        Summary = intent.Summary,
                        SuggestedTargetBiome = intent.SuggestedTargetBiome,
                    })
                    .ToList(),
            },
            Narrative = new HeroJourneySaveData
            {
                CurrentStage = runtimeState.Narrative.CurrentStage,
                LastStageReason = runtimeState.Narrative.LastStageReason,
                LastAdvancedUtc = runtimeState.Narrative.LastAdvancedUtc,
                StageHistory = runtimeState.Narrative.StageHistory
                    .Select(stage => new HeroJourneyStageRecordSaveData
                    {
                        Stage = stage.Stage,
                        TimestampUtc = stage.TimestampUtc,
                        Reason = stage.Reason,
                    })
                    .ToList(),
            },
            Omen = new OmenSaveData
            {
                CooldownSeconds = runtimeState.Omen.CooldownSeconds,
                VisualSeconds = runtimeState.Omen.VisualSeconds,
                LastScore = runtimeState.Omen.LastScore,
                LastSource = runtimeState.Omen.LastSource,
                ActiveOmen = runtimeState.Omen.ActiveOmen is null
                    ? null
                    : new OmenRecordSaveData
                    {
                        TimestampUtc = runtimeState.Omen.ActiveOmen.TimestampUtc,
                        OmenType = runtimeState.Omen.ActiveOmen.OmenType,
                        Source = runtimeState.Omen.ActiveOmen.Source,
                        Score = runtimeState.Omen.ActiveOmen.Score,
                        Description = runtimeState.Omen.ActiveOmen.Description,
                    },
                History = runtimeState.Omen.History
                    .Select(omen => new OmenRecordSaveData
                    {
                        TimestampUtc = omen.TimestampUtc,
                        OmenType = omen.OmenType,
                        Source = omen.Source,
                        Score = omen.Score,
                        Description = omen.Description,
                    })
                    .ToList(),
            },
            Perception = new PerceptionSaveData
            {
                IsActive = runtimeState.Perception.IsActive,
                ActiveSecondsRemaining = runtimeState.Perception.ActiveSecondsRemaining,
                CooldownSecondsRemaining = runtimeState.Perception.CooldownSecondsRemaining,
                Intensity = runtimeState.Perception.Intensity,
                ActivationCount = runtimeState.Perception.ActivationCount,
                LastActivatedUtc = runtimeState.Perception.LastActivatedUtc,
            },
        };
    }

    private static void ClampRuntimeState(WorldLawRuntimeState runtimeState)
    {
        runtimeState.World.ExplorationDrive = Clamp01(runtimeState.World.ExplorationDrive);
        runtimeState.World.BorderLonging = Clamp01(runtimeState.World.BorderLonging);
        runtimeState.World.BlessingWeight = Clamp01(runtimeState.World.BlessingWeight);
        runtimeState.World.ResourcePressure = Clamp01(runtimeState.World.ResourcePressure);
        runtimeState.World.Karma = MathUtil.Clamp(runtimeState.World.Karma, -1f, 1f);
        runtimeState.World.Violence = Clamp01(runtimeState.World.Violence);
        runtimeState.World.Faith = Clamp01(runtimeState.World.Faith);
        runtimeState.World.Curiosity = Clamp01(runtimeState.World.Curiosity);
        runtimeState.World.PathVisibility = Clamp01(runtimeState.World.PathVisibility);
        runtimeState.World.SocialFlux = Clamp01(runtimeState.World.SocialFlux);
        runtimeState.World.Atmosphere = Clamp01(runtimeState.World.Atmosphere);
        runtimeState.World.LossMemory = Clamp01(runtimeState.World.LossMemory);

        runtimeState.Behavior.PeacefulTendency = Clamp01(runtimeState.Behavior.PeacefulTendency);
        runtimeState.Behavior.ViolentTendency = Clamp01(runtimeState.Behavior.ViolentTendency);
        runtimeState.Behavior.ExplorationTendency = Clamp01(runtimeState.Behavior.ExplorationTendency);
        runtimeState.Behavior.FaithTendency = Clamp01(runtimeState.Behavior.FaithTendency);
        runtimeState.Behavior.CuriosityTendency = Clamp01(runtimeState.Behavior.CuriosityTendency);

        if (runtimeState.RecentCausality is null)
        {
            runtimeState.RecentCausality = new List<CausalityRecord>();
        }

        runtimeState.Intent ??= new PlayerIntentRuntimeState();
        runtimeState.Intent.RecentIntents ??= new List<PlayerIntentRecord>();

        runtimeState.Narrative ??= new HeroJourneyRuntimeState();
        runtimeState.Narrative.StageHistory ??= new List<HeroJourneyStageRecord>();
        runtimeState.Omen ??= new OmenRuntimeState();
        runtimeState.Omen.History ??= new List<OmenRecord>();
        runtimeState.Perception ??= new PerceptionRuntimeState();
    }

    private static float Clamp01(float value)
    {
        return MathUtil.Clamp(value, 0f, 1f);
    }
}
