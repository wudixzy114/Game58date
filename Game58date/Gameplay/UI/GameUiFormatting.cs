#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Gameplay.UI;

public static class GameUiFormatting
{
    public static string GetIntentTopicTitle(IntentTopic topic)
    {
        return topic switch
        {
            IntentTopic.Exploration => "Exploration",
            IntentTopic.Mentor => "Mentor",
            IntentTopic.Knowledge => "Knowledge",
            IntentTopic.Compassion => "Compassion",
            IntentTopic.Domination => "Domination",
            _ => "Unknown",
        };
    }

    public static string GetOmenSourceTitle(OmenSource source)
    {
        return source switch
        {
            OmenSource.Intent => "Intent",
            OmenSource.EmergentWorldLaw => "World",
            OmenSource.Causality => "Causality",
            OmenSource.Narrative => "Narrative",
            _ => "None",
        };
    }

    public static string AsPercent(float value)
    {
        return $"{MathUtil.Clamp(value, 0f, 1f) * 100f:0}%";
    }

    public static string AsSignedPercent(float normalizedMinusOneToOne)
    {
        float clamped = MathUtil.Clamp(normalizedMinusOneToOne, -1f, 1f);
        return $"{clamped * 100f:+0;-0;0}%";
    }
}
