#nullable enable
using System.Collections.Generic;

namespace Game58date.Gameplay.UI;

public sealed class GameUiContextState
{
    public GameUiMode Mode { get; set; } = GameUiMode.Exploration;

    public string ModeTag { get; set; } = "Context HUD";

    public string Subtitle { get; set; } =
        "Contextual ritual HUD. The world stays visually quiet until the journey truly demands a response.";

    public string HelpText { get; set; } =
        "Enter submit  Tab input  Q sense  F2 sea  F3 loss  F4 violent  F5 peaceful  F6 mentor  Esc pause  F9 hide UI";

    public string IntentDraftText { get; set; } = string.Empty;

    public bool MenuVisible { get; set; }

    public bool UiHidden { get; set; }

    public bool DebugHudVisible { get; set; }

    public string MenuTitle { get; set; } = "System Pause";

    public string MenuHintText { get; set; } =
        "Esc / F10 pauses and releases the mouse. R returns to the router. F9 hides every runtime UI layer.";

    public List<GameUiNoticeRecord> Notices { get; } = new();
}
