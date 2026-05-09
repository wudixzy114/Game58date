#nullable enable
using System;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay;

public sealed class OmenDirector
{
    private const int MaxHistory = 24;

    public OmenDirector(OmenRuntimeState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public OmenRuntimeState State { get; }

    public event Action<OmenRecord>? OmenActivated;

    public void Tick(float deltaTimeSeconds, WorldLawRuntimeState runtimeState)
    {
        if (deltaTimeSeconds <= 0f)
        {
            return;
        }

        State.CooldownSeconds = MathF.Max(0f, State.CooldownSeconds - deltaTimeSeconds);
        State.VisualSeconds = MathF.Max(0f, State.VisualSeconds - deltaTimeSeconds);

        if (State.CooldownSeconds > 0f)
        {
            return;
        }

        OmenRecord? emergent = EvaluateEmergentOmen(runtimeState);
        if (emergent is null)
        {
            return;
        }

        Activate(emergent);
    }

    public void NotifyIntent(PlayerIntentRecord intentRecord, WorldLawRuntimeState runtimeState)
    {
        OmenRecord? intentOmen = EvaluateIntentOmen(intentRecord, runtimeState);
        if (intentOmen is null)
        {
            return;
        }

        Activate(intentOmen);
    }

    public void NotifyCausality(CausalityRecord record, WorldLawRuntimeState runtimeState)
    {
        OmenRecord? causalityOmen = EvaluateCausalityOmen(record, runtimeState);
        if (causalityOmen is null)
        {
            return;
        }

        Activate(causalityOmen);
    }

    public void NotifyNarrative(HeroJourneyStage stage, string reason, WorldLawRuntimeState runtimeState)
    {
        OmenRecord? narrativeOmen = EvaluateNarrativeOmen(stage, reason, runtimeState);
        if (narrativeOmen is null)
        {
            return;
        }

        Activate(narrativeOmen);
    }

    private OmenRecord? EvaluateIntentOmen(PlayerIntentRecord intentRecord, WorldLawRuntimeState runtimeState)
    {
        return intentRecord.Topic switch
        {
            IntentTopic.Exploration => Create(OmenType.PathRevelation, OmenSource.Intent, 0.74f + intentRecord.Confidence * 0.18f, "The horizon answered the player's travel intent with a navigable sign."),
            IntentTopic.Mentor => Create(OmenType.GuideArrival, OmenSource.Intent, 0.76f + intentRecord.Confidence * 0.16f, "A guidance omen formed after the player asked for a mentor."),
            IntentTopic.Knowledge => Create(OmenType.Divination, OmenSource.Intent, 0.72f + intentRecord.Confidence * 0.18f, "The world reflected the search for hidden knowledge."),
            IntentTopic.Compassion => Create(OmenType.SocialShift, OmenSource.Intent, 0.70f + intentRecord.Confidence * 0.15f, "Nearby social order softened in response to compassionate intent."),
            IntentTopic.Domination => Create(OmenType.NaturalAnomaly, OmenSource.Intent, 0.73f + intentRecord.Confidence * 0.17f, "The environment reacted harshly to a dominance-driven intent."),
            _ => null,
        };
    }

    private OmenRecord? EvaluateEmergentOmen(WorldLawRuntimeState runtimeState)
    {
        WorldLawState world = runtimeState.World;
        float score = 0f;
        OmenType omenType = OmenType.None;
        string description = string.Empty;

        if (world.ResourcePressure > 0.72f)
        {
            score = 0.78f + world.ResourcePressure * 0.18f;
            omenType = OmenType.NaturalAnomaly;
            description = "Resource pressure climbed high enough to produce a hostile natural omen.";
        }
        else if (world.BlessingWeight > 0.65f)
        {
            score = 0.76f + world.BlessingWeight * 0.15f;
            omenType = OmenType.PathRevelation;
            description = "Blessing momentum bent the world toward a path-revealing omen.";
        }
        else if (world.Karma < -0.55f)
        {
            score = 0.75f + MathF.Abs(world.Karma) * 0.14f;
            omenType = OmenType.NaturalAnomaly;
            description = "Negative karma accumulated into a visible environmental backlash.";
        }
        else if (world.Karma > 0.55f && runtimeState.Behavior.PeacefulTendency > 0.24f)
        {
            score = 0.72f + world.Karma * 0.14f;
            omenType = OmenType.GuideArrival;
            description = "Sustained positive karma called a patient guiding omen into view.";
        }
        else if (world.ExplorationDrive > 0.68f && runtimeState.Intent.SubmittedIntentCount > 0)
        {
            score = 0.71f + world.ExplorationDrive * 0.12f;
            omenType = OmenType.SocialShift;
            description = "Exploration pressure reshaped distant routes and travel signals.";
        }

        if (omenType == OmenType.None)
        {
            return null;
        }

        return Create(omenType, OmenSource.EmergentWorldLaw, score, description);
    }

    private OmenRecord? EvaluateCausalityOmen(CausalityRecord record, WorldLawRuntimeState runtimeState)
    {
        if (record.ActionKind == PlayerActionKind.LossRegistered && runtimeState.World.BlessingWeight > 0.24f)
        {
            return Create(OmenType.Divination, OmenSource.Causality, 0.81f, "A recent loss tipped causality toward a divinatory omen.");
        }

        if (record.ActionKind == PlayerActionKind.ViolentChoice && runtimeState.World.ResourcePressure > 0.18f)
        {
            return Create(OmenType.NaturalAnomaly, OmenSource.Causality, 0.79f, "Violent causality amplified local instability into a natural anomaly.");
        }

        if (record.ActionKind == PlayerActionKind.PeacefulChoice && runtimeState.Behavior.PeacefulTendency > 0.20f)
        {
            return Create(OmenType.SocialShift, OmenSource.Causality, 0.77f, "Repeated peaceful choices changed the social weather around the player.");
        }

        return null;
    }

    private OmenRecord? EvaluateNarrativeOmen(HeroJourneyStage stage, string reason, WorldLawRuntimeState runtimeState)
    {
        return stage switch
        {
            HeroJourneyStage.CallToAdventure => Create(OmenType.PathRevelation, OmenSource.Narrative, 0.78f, $"Narrative omen: {reason}"),
            HeroJourneyStage.CrossingTheThreshold => Create(OmenType.Divination, OmenSource.Narrative, 0.80f, $"Narrative omen: {reason}"),
            HeroJourneyStage.RoadOfTrials => Create(OmenType.SocialShift, OmenSource.Narrative, 0.79f, $"Narrative omen: {reason}"),
            HeroJourneyStage.MeetingTheMentor => Create(OmenType.GuideArrival, OmenSource.Narrative, 0.84f, $"Narrative omen: {reason}"),
            HeroJourneyStage.ApproachToTheInmostCave => Create(OmenType.Divination, OmenSource.Narrative, 0.83f, $"Narrative omen: {reason}"),
            HeroJourneyStage.Transformation => Create(OmenType.PathRevelation, OmenSource.Narrative, 0.88f, $"Narrative omen: {reason}"),
            _ => null,
        };
    }

    private void Activate(OmenRecord omenRecord)
    {
        State.CooldownSeconds = 8f;
        State.VisualSeconds = 6f;
        State.LastScore = omenRecord.Score;
        State.LastSource = omenRecord.Source;
        State.ActiveOmen = omenRecord;
        State.History.Add(omenRecord);

        while (State.History.Count > MaxHistory)
        {
            State.History.RemoveAt(0);
        }

        OmenActivated?.Invoke(omenRecord);
    }

    private static OmenRecord Create(OmenType omenType, OmenSource source, float score, string description)
    {
        return new OmenRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            OmenType = omenType,
            Source = source,
            Score = MathUtil.Clamp(score, 0f, 1f),
            Description = description,
        };
    }
}
