#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Gameplay.UI;

public sealed class GameUiNoticeRecord
{
    public string TitleText { get; set; } = string.Empty;

    public string BodyText { get; set; } = string.Empty;

    public GameUiNoticeSeverity Severity { get; set; } = GameUiNoticeSeverity.Info;

    public Color AccentColor { get; set; }

    public Color FillColor { get; set; }

    public float RemainingSeconds { get; set; }
}
