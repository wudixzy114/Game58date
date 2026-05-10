#nullable enable
using System;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay.UI;

public sealed class MainMenuViewState
{
    public string HeadingText { get; set; } = "GAME58DATE";

    public string SubtitleText { get; set; } =
        "An AI-guided hero's journey through omens, laws, and landscapes that answer intention instead of quest markers.";

    public string WorldPulseText { get; set; } = "Current World Pulse";

    public string AtmosphereText { get; set; } = string.Empty;

    public string OmenText { get; set; } = string.Empty;

    public string JourneyText { get; set; } = string.Empty;

    public string FooterHintText { get; set; } =
        "Up/Down to move  Enter to launch  Esc returns to the development router";

    public MainMenuOptionViewState[] Options { get; set; } = Array.Empty<MainMenuOptionViewState>();

    public Color AtmosphereAccentColor { get; set; }
}

public sealed class MainMenuOptionViewState
{
    public MainMenuAction Action { get; set; }

    public string LabelText { get; set; } = string.Empty;

    public string DescriptionText { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
