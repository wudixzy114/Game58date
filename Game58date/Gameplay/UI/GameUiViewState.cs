#nullable enable
using System;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay.UI;

public sealed class GameUiViewState
{
    public string ModeTagText { get; set; } = string.Empty;

    public string TitleText { get; set; } = string.Empty;

    public string SubtitleText { get; set; } = string.Empty;

    public string StageText { get; set; } = string.Empty;

    public string BiomeText { get; set; } = string.Empty;

    public string OmenText { get; set; } = string.Empty;

    public string OmenDetailText { get; set; } = string.Empty;

    public string ProfileText { get; set; } = string.Empty;

    public string PerceptionText { get; set; } = string.Empty;

    public string IntentText { get; set; } = string.Empty;

    public string HelpText { get; set; } = string.Empty;

    public string InputHintText { get; set; } = string.Empty;

    public string IntentDraftText { get; set; } = string.Empty;

    public string LastIntentSummaryText { get; set; } = string.Empty;

    public string NarrativeTitleText { get; set; } = "Journey Logic";

    public string NarrativeReasonText { get; set; } = string.Empty;

    public string HistoryTitleText { get; set; } = "Recent Signals";

    public string[] HistoryLines { get; set; } = Array.Empty<string>();

    public GameUiMeterViewState KarmaMeter { get; set; } = new();

    public GameUiMeterViewState BlessingMeter { get; set; } = new();

    public GameUiMeterViewState PathMeter { get; set; } = new();

    public GameUiMeterViewState DangerMeter { get; set; } = new();

    public bool MenuVisible { get; set; }

    public bool InputEnabled { get; set; }

    public string MenuTitleText { get; set; } = "System Atlas";

    public string MenuStageText { get; set; } = string.Empty;

    public string MenuMetaText { get; set; } = string.Empty;

    public string MenuSettingsText { get; set; } = string.Empty;

    public string MenuHintText { get; set; } = string.Empty;

    public string MenuToggleButtonText { get; set; } = "Open Atlas";

    public string NarrativeInputButtonText { get; set; } = "Enable Input";

    public string DebugHudButtonText { get; set; } = "Show Debug HUD";

    public string ModeSummaryText { get; set; } = string.Empty;

    public string JourneySummaryText { get; set; } = string.Empty;

    public string WorldPulseSummaryText { get; set; } = string.Empty;

    public GameUiNoticeRecord[] Notices { get; set; } = Array.Empty<GameUiNoticeRecord>();

    public Color ModeTagFillColor { get; set; }

    public Color ModeTagTextColor { get; set; }

    public Color OmenAccentColor { get; set; }

    public Color InputBorderColor { get; set; }

    public Color InputFillColor { get; set; }
}

public sealed class GameUiMeterViewState
{
    public string LabelText { get; set; } = string.Empty;

    public string ValueText { get; set; } = string.Empty;

    public string SummaryText { get; set; } = string.Empty;

    public float FillRatio { get; set; }

    public Color FillColor { get; set; }
}
