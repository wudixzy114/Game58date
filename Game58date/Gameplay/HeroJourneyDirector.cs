#nullable enable
using System;

namespace Game58date.Gameplay;

public sealed class HeroJourneyDirector
{
    private const int MaxStageHistory = 24;

    public HeroJourneyDirector(HeroJourneyRuntimeState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public HeroJourneyRuntimeState State { get; }

    public event Action<HeroJourneyStage, string>? StageAdvanced;

    public void Evaluate(WorldLawRuntimeState runtimeState)
    {
        HeroJourneyStage current = State.CurrentStage;
        HeroJourneyStage next = current;
        string reason = State.LastStageReason;

        WorldLawState world = runtimeState.World;
        PlayerBehaviorProfile behavior = runtimeState.Behavior;
        PlayerIntentRecord? intent = runtimeState.Intent.LastIntent;

        if (world.IntentCount >= 1 && world.ExplorationDrive > 0.12f)
        {
            next = HeroJourneyStage.CallToAdventure;
            reason = "The player expressed an explicit intent and exploration drive crossed the call threshold.";
        }

        if (world.OmenCount >= 1 && world.Curiosity > 0.26f)
        {
            next = HeroJourneyStage.CrossingTheThreshold;
            reason = "The first omen landed while curiosity stayed elevated.";
        }

        if (world.BorderLonging > 0.40f || world.PathVisibility > 0.45f || intent?.Topic == IntentTopic.Exploration)
        {
            next = HeroJourneyStage.RoadOfTrials;
            reason = "Border longing, visible paths, or long-range travel intent moved the journey into active trials.";
        }

        if (world.Faith > 0.45f && world.Karma > 0.10f && (intent?.Topic == IntentTopic.Mentor || behavior.PeacefulTendency > 0.28f))
        {
            next = HeroJourneyStage.MeetingTheMentor;
            reason = "Faith, positive karma, and guidance-seeking behavior aligned with a mentor encounter.";
        }

        if (world.PathVisibility > 0.70f && world.Curiosity > 0.55f && runtimeState.Intent.SubmittedIntentCount >= 2)
        {
            next = HeroJourneyStage.ApproachToTheInmostCave;
            reason = "Paths are clear, curiosity is high, and repeated intent submissions indicate commitment.";
        }

        if (world.BlessingWeight > 0.55f && world.OmenCount >= 3 && runtimeState.Behavior.RecordedActions >= 5)
        {
            next = HeroJourneyStage.Transformation;
            reason = "Blessing momentum, repeated omens, and sufficient behavioral history unlocked transformation.";
        }

        if (next <= current)
        {
            return;
        }

        State.CurrentStage = next;
        State.LastStageReason = reason;
        State.LastAdvancedUtc = DateTimeOffset.UtcNow;
        State.StageHistory.Add(new HeroJourneyStageRecord
        {
            Stage = next,
            TimestampUtc = State.LastAdvancedUtc.Value,
            Reason = reason,
        });

        while (State.StageHistory.Count > MaxStageHistory)
        {
            State.StageHistory.RemoveAt(0);
        }

        StageAdvanced?.Invoke(next, reason);
    }
}
