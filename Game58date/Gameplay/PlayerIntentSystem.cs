#nullable enable
using System;
using System.Linq;

namespace Game58date.Gameplay;

public sealed class PlayerIntentSystem
{
    private const int MaxIntentHistory = 16;

    public PlayerIntentSystem(PlayerIntentRuntimeState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public PlayerIntentRuntimeState State { get; }

    public PlayerIntentRecord? Submit(string rawIntent)
    {
        string intent = rawIntent.Trim();
        if (string.IsNullOrWhiteSpace(intent))
        {
            return null;
        }

        PlayerIntentRecord record = Analyze(intent);
        State.SubmittedIntentCount++;
        State.LastIntent = record;
        State.RecentIntents.Add(record);

        while (State.RecentIntents.Count > MaxIntentHistory)
        {
            State.RecentIntents.RemoveAt(0);
        }

        return record;
    }

    private static PlayerIntentRecord Analyze(string intent)
    {
        string normalized = intent.ToLowerInvariant();
        IntentTopic topic = IntentTopic.Unknown;
        float confidence = 0.42f;
        string summary = "Unclassified exploration intent.";
        string suggestedBiome = string.Empty;

        if (ContainsAny(normalized, "sea", "cross", "shore", "ocean", "ship", "harbor"))
        {
            topic = IntentTopic.Exploration;
            confidence = 0.92f;
            summary = "Long-range exploration intent focused on crossing boundaries.";
            suggestedBiome = "Desert Crossing";
        }
        else if (ContainsAny(normalized, "mentor", "guide", "teacher", "sage", "sign"))
        {
            topic = IntentTopic.Mentor;
            confidence = 0.90f;
            summary = "Guidance-seeking intent focused on mentors and signs.";
            suggestedBiome = "Pilgrim Road";
        }
        else if (ContainsAny(normalized, "treasure", "relic", "knowledge", "ruin", "secret"))
        {
            topic = IntentTopic.Knowledge;
            confidence = 0.88f;
            summary = "Discovery intent focused on relics, secrets, or ruins.";
            suggestedBiome = "Ruined Basin";
        }
        else if (ContainsAny(normalized, "peace", "help", "save", "heal", "kind"))
        {
            topic = IntentTopic.Compassion;
            confidence = 0.86f;
            summary = "Compassion intent focused on peace, aid, or healing.";
            suggestedBiome = "Market Refuge";
        }
        else if (ContainsAny(normalized, "kill", "take", "destroy", "rule", "loot"))
        {
            topic = IntentTopic.Domination;
            confidence = 0.87f;
            summary = "Dominance intent focused on violence, control, or plunder.";
            suggestedBiome = "Ash Frontier";
        }
        else if (intent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 4)
        {
            topic = IntentTopic.Exploration;
            confidence = 0.58f;
            summary = "General exploratory intent with no dominant semantic tag.";
        }

        return new PlayerIntentRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            RawText = intent,
            Topic = topic,
            Confidence = confidence,
            Summary = summary,
            SuggestedTargetBiome = suggestedBiome,
        };
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        return keywords.Any(keyword => source.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
