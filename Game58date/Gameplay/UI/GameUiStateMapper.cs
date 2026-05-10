#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay.UI;

public static class GameUiStateMapper
{
    public static GameUiViewState Map(WorldLawRuntimeState state, GameUiContextState context, GameUiTheme theme)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (theme is null)
        {
            throw new ArgumentNullException(nameof(theme));
        }

        OmenType omenType = state.Omen.ActiveOmen?.OmenType ?? state.World.LastOmen;
        OmenSource omenSource = state.Omen.ActiveOmen?.Source ?? state.Omen.LastSource;
        Color omenTint = GetOmenTint(omenType, theme);
        bool inputEnabled = state.Intent.TextInputEnabled;
        float danger = MathF.Max(state.World.ResourcePressure, state.Behavior.ViolentTendency);
        float karmaRatio = (MathUtil.Clamp(state.World.Karma, -1f, 1f) + 1f) * 0.5f;

        return new GameUiViewState
        {
            ModeTagText = context.ModeTag,
            ModeSummaryText = context.Mode switch
            {
                GameUiMode.Exploration => "Exploration",
                GameUiMode.InputFocus => "Narrative Input",
                GameUiMode.Atlas => "System Atlas",
                GameUiMode.MainMenu => "Main Menu",
                GameUiMode.Showcase => "Showcase",
                GameUiMode.Routing => "Development Router",
                _ => "Unknown",
            },
            TitleText = "GAME58DATE",
            SubtitleText = context.Subtitle,
            StageText = $"Hero Journey: {WorldLawEngine.GetStageTitle(state.Narrative.CurrentStage)}",
            BiomeText = $"Target Biome: {state.World.TargetBiome}",
            OmenText = $"Omen: {WorldLawEngine.GetOmenTitle(omenType)}  {GameUiFormatting.AsPercent(state.Omen.ActiveOmen?.Score ?? state.Omen.LastScore)}  Source {GameUiFormatting.GetOmenSourceTitle(omenSource)}",
            OmenDetailText = string.IsNullOrWhiteSpace(state.Omen.ActiveOmen?.Description)
                ? state.World.LastOmenDescription
                : state.Omen.ActiveOmen!.Description,
            ProfileText =
                $"Profile  Peace {GameUiFormatting.AsPercent(state.Behavior.PeacefulTendency)}  Violence {GameUiFormatting.AsPercent(state.Behavior.ViolentTendency)}  Faith {GameUiFormatting.AsPercent(state.Behavior.FaithTendency)}  Curiosity {GameUiFormatting.AsPercent(state.Behavior.CuriosityTendency)}",
            PerceptionText =
                $"Perception: {(state.Perception.IsActive ? "Awakened" : "Dormant")}  Intensity {GameUiFormatting.AsPercent(state.Perception.Intensity)}  Remaining {state.Perception.ActiveSecondsRemaining:0.0}s  Cooldown {state.Perception.CooldownSecondsRemaining:0.0}s",
            IntentText =
                $"Intent: {GameUiFormatting.GetIntentTopicTitle(state.Intent.LastIntent?.Topic ?? IntentTopic.Unknown)}  Confidence {(state.Intent.LastIntent?.Confidence ?? 0f) * 100f:0}%  Total {state.Intent.SubmittedIntentCount}",
            HelpText = context.HelpText,
            InputHintText = inputEnabled
                ? "Write a destination, vow, warning, or question. The clean exploration surface only yields when your intent becomes meaningful."
                : "Narrative input is muted. The world remains quiet until you reopen the ritual channel.",
            IntentDraftText = context.IntentDraftText,
            LastIntentSummaryText = state.Intent.LastIntent?.Summary ?? "No structured intent has been submitted yet.",
            NarrativeReasonText = state.Narrative.LastStageReason,
            HistoryTitleText = "Recent Signals",
            HistoryLines = BuildHistoryLines(state),
            KarmaMeter = new GameUiMeterViewState
            {
                LabelText = "Karma Arc",
                ValueText = GameUiFormatting.AsSignedPercent(state.World.Karma),
                SummaryText = state.World.Karma >= 0f ? "Grace remains in the causal field." : "Backlash is accumulating beneath the surface.",
                FillRatio = karmaRatio,
                FillColor = state.World.Karma >= 0f ? theme.Positive : theme.Danger,
            },
            BlessingMeter = new GameUiMeterViewState
            {
                LabelText = "Blessing Weight",
                ValueText = GameUiFormatting.AsPercent(state.World.BlessingWeight),
                SummaryText = "Hidden fortune after loss and hardship.",
                FillRatio = state.World.BlessingWeight,
                FillColor = theme.AccentGold,
            },
            PathMeter = new GameUiMeterViewState
            {
                LabelText = "Path Visibility",
                ValueText = GameUiFormatting.AsPercent(state.World.PathVisibility),
                SummaryText = "How clearly the world reveals the next threshold.",
                FillRatio = state.World.PathVisibility,
                FillColor = theme.AccentCyan,
            },
            DangerMeter = new GameUiMeterViewState
            {
                LabelText = "Pressure Index",
                ValueText = GameUiFormatting.AsPercent(danger),
                SummaryText = "Resource stress, violence, and unstable atmosphere.",
                FillRatio = danger,
                FillColor = danger >= 0.58f ? theme.Danger : theme.Warning,
            },
            MenuVisible = context.MenuVisible,
            InputEnabled = inputEnabled,
            MenuTitleText = "System Atlas",
            MenuStageText = $"Current Stage: {WorldLawEngine.GetStageTitle(state.Narrative.CurrentStage)}",
            MenuMetaText =
                $"Signals {state.Omen.History.Count}  Intents {state.Intent.SubmittedIntentCount}  Perception Uses {state.Perception.ActivationCount}  Actions {state.Behavior.RecordedActions}",
            MenuSettingsText =
                $"Display: Contextual Ritual HUD  |  Perception Boost: {(state.Perception.IsActive ? "Active" : "Passive")}  |  Input Focus: {(inputEnabled ? "Narrative" : "Travel")}  |  Debug HUD: {(context.DebugHudVisible ? "Visible" : "Hidden")}",
            MenuHintText = context.MenuHintText,
            MenuToggleButtonText = context.MenuVisible ? "Close Atlas" : "Open Atlas",
            NarrativeInputButtonText = inputEnabled ? "Mute Input" : "Enable Input",
            DebugHudButtonText = context.DebugHudVisible ? "Hide Debug HUD" : "Show Debug HUD",
            JourneySummaryText = state.Narrative.LastStageReason,
            WorldPulseSummaryText = $"Omens {state.Omen.History.Count}  Pressure {GameUiFormatting.AsPercent(state.World.ResourcePressure)}  Blessing {GameUiFormatting.AsPercent(state.World.BlessingWeight)}",
            ModeTagFillColor = state.Perception.IsActive ? theme.AccentCyan : omenTint,
            ModeTagTextColor = theme.PanelBase,
            OmenAccentColor = omenTint,
            InputBorderColor = inputEnabled
                ? (state.Perception.IsActive ? theme.AccentCyan : omenTint)
                : theme.TextDisabled,
            InputFillColor = inputEnabled
                ? (state.Perception.IsActive ? theme.PanelElevated : theme.InputFill)
                : theme.PanelBase,
            Notices = context.Notices.ToArray(),
        };
    }

    private static string[] BuildHistoryLines(WorldLawRuntimeState state)
    {
        var lines = new List<string>(3);

        for (int index = state.Omen.History.Count - 1; index >= 0 && lines.Count < 3; index--)
        {
            OmenRecord omen = state.Omen.History[index];
            lines.Add($"{WorldLawEngine.GetOmenTitle(omen.OmenType)}  {GameUiFormatting.AsPercent(omen.Score)}  {GameUiFormatting.GetOmenSourceTitle(omen.Source)}");
        }

        if (lines.Count < 3 && state.RecentCausality.Count > 0)
        {
            CausalityRecord causality = state.RecentCausality[^1];
            lines.Add($"Causality  {causality.ActionKind}  {causality.Summary}");
        }

        if (lines.Count < 3 && state.Intent.LastIntent is not null)
        {
            lines.Add($"Intent Echo  {GameUiFormatting.GetIntentTopicTitle(state.Intent.LastIntent.Topic)}  {state.Intent.LastIntent.Summary}");
        }

        while (lines.Count < 3)
        {
            lines.Add(lines.Count == 0
                ? "No major omen has resolved into visible history yet."
                : string.Empty);
        }

        return lines.ToArray();
    }

    private static Color GetOmenTint(OmenType omenType, GameUiTheme theme)
    {
        return omenType switch
        {
            OmenType.NaturalAnomaly => theme.AccentEmber,
            OmenType.SocialShift => theme.Warning,
            OmenType.GuideArrival => theme.AccentVerdant,
            OmenType.Divination => theme.AccentCyan,
            OmenType.PathRevelation => theme.AccentGold,
            _ => theme.PanelBorder,
        };
    }
}
