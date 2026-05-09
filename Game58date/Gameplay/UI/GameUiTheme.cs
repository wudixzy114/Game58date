#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Gameplay.UI;

public sealed class GameUiTheme
{
    public static readonly GameUiTheme Default = new();

    public Color AccentGold { get; } = new(0.93f, 0.78f, 0.46f, 1f);

    public Color AccentCyan { get; } = new(0.40f, 0.82f, 0.93f, 1f);

    public Color AccentVerdant { get; } = new(0.53f, 0.89f, 0.68f, 1f);

    public Color AccentEmber { get; } = new(0.95f, 0.48f, 0.36f, 1f);

    public Color TextPrimary { get; } = new(0.96f, 0.94f, 0.90f, 1f);

    public Color TextMuted { get; } = new(0.73f, 0.71f, 0.67f, 1f);

    public Color TextDisabled { get; } = new(0.44f, 0.43f, 0.40f, 1f);

    public Color PanelBase { get; } = new(0.055f, 0.060f, 0.072f, 0.92f);

    public Color PanelElevated { get; } = new(0.095f, 0.102f, 0.118f, 0.96f);

    public Color PanelBorder { get; } = new(0.46f, 0.39f, 0.25f, 0.82f);

    public Color PanelOrnament { get; } = new(0.32f, 0.27f, 0.17f, 0.55f);

    public Color BackgroundVeil { get; } = new(0.015f, 0.018f, 0.028f, 0.46f);

    public Color BackgroundPattern { get; } = new(0.74f, 0.67f, 0.50f, 0.08f);

    public Color InputFill { get; } = new(0.11f, 0.12f, 0.14f, 0.98f);

    public Color Positive { get; } = new(0.52f, 0.87f, 0.63f, 1f);

    public Color Warning { get; } = new(0.97f, 0.73f, 0.40f, 1f);

    public Color Danger { get; } = new(0.94f, 0.45f, 0.37f, 1f);
}
