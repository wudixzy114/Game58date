#nullable enable
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Game58date.Gameplay.UI;

public sealed class MainMenuComposer
{
    private readonly GameUiTheme theme;
    private readonly IMainMenuActionSink actionSink;
    private readonly SpriteFont font;

    private UIComponent? uiComponent;
    private TextBlock? headingText;
    private TextBlock? subtitleText;
    private TextBlock? worldPulseText;
    private TextBlock? atmosphereText;
    private TextBlock? omenText;
    private TextBlock? journeyText;
    private TextBlock? footerHintText;
    private Border? pulseCard;
    private MainMenuOptionWidgets[] optionWidgets = Array.Empty<MainMenuOptionWidgets>();

    public MainMenuComposer(GameUiTheme theme, IServiceRegistry services, IMainMenuActionSink actionSink)
    {
        this.theme = theme ?? throw new ArgumentNullException(nameof(theme));
        this.actionSink = actionSink ?? throw new ArgumentNullException(nameof(actionSink));
        font = GameUiFontProvider.Load(services);
    }

    public void Attach(Entity owner)
    {
        uiComponent = owner.Get<UIComponent>();
        if (uiComponent is null)
        {
            uiComponent = new UIComponent
            {
                Resolution = new Vector3(1920f, 1080f, 1000f),
            };
            owner.Add(uiComponent);
        }

        uiComponent.Page = new UIPage
        {
            RootElement = BuildRoot(),
        };
    }

    public void Update(MainMenuViewState state)
    {
        SetText(headingText, state.HeadingText);
        SetText(subtitleText, state.SubtitleText);
        SetText(worldPulseText, state.WorldPulseText);
        SetText(atmosphereText, state.AtmosphereText);
        SetText(omenText, state.OmenText);
        SetText(journeyText, state.JourneyText);
        SetText(footerHintText, state.FooterHintText);

        if (pulseCard is not null)
        {
            pulseCard.BorderColor = state.AtmosphereAccentColor;
        }

        for (int i = 0; i < optionWidgets.Length && i < state.Options.Length; i++)
        {
            MainMenuOptionViewState option = state.Options[i];
            MainMenuOptionWidgets widgets = optionWidgets[i];
            widgets.Label.Text = option.IsSelected ? $"> {option.LabelText}" : option.LabelText;
            widgets.Description.Text = option.DescriptionText;
            widgets.Border.BorderColor = option.IsSelected ? theme.AccentGold : theme.PanelBorder;
            widgets.Border.BackgroundColor = option.IsSelected ? new Color(0.17f, 0.14f, 0.10f, 0.96f) : theme.PanelBase;
        }
    }

    private UIElement BuildRoot()
    {
        Canvas root = new()
        {
            Width = 1920f,
            Height = 1080f,
        };

        Border veil = new()
        {
            Width = 1920f,
            Height = 1080f,
            BackgroundColor = new Color(0.02f, 0.03f, 0.04f, 0.40f),
            BorderThickness = new Thickness(0f, 0f, 0f, 0f),
        };
        UIElementExtensions.SetCanvasPinOrigin(veil, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(veil, new Vector3(0f, 0f, 0f));
        root.Children.Add(veil);

        Border heroCard = new()
        {
            Width = 780f,
            Height = 340f,
            BackgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.86f),
            BorderColor = theme.PanelBorder,
            BorderThickness = new Thickness(2f, 2f, 2f, 2f),
        };
        UIElementExtensions.SetCanvasPinOrigin(heroCard, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(heroCard, new Vector3(0.06f, 0.08f, 0f));
        root.Children.Add(heroCard);

        StackPanel heroStack = new()
        {
            Orientation = Orientation.Vertical,
            Width = 700f,
            Height = 260f,
            Margin = new Thickness(34f, 34f, 34f, 34f),
        };
        heroCard.Content = heroStack;

        headingText = CreateText("", 54f, theme.TextPrimary);
        subtitleText = CreateText("", 17f, theme.TextMuted);
        subtitleText.WrapText = true;
        subtitleText.Height = 84f;
        heroStack.Children.Add(headingText);
        heroStack.Children.Add(subtitleText);

        pulseCard = new Border
        {
            Width = 560f,
            Height = 180f,
            BackgroundColor = new Color(0.08f, 0.09f, 0.11f, 0.90f),
            BorderColor = theme.AccentGold,
            BorderThickness = new Thickness(2f, 2f, 2f, 2f),
        };
        UIElementExtensions.SetCanvasPinOrigin(pulseCard, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(pulseCard, new Vector3(0.66f, 0.10f, 0f));
        root.Children.Add(pulseCard);

        StackPanel pulseStack = new()
        {
            Orientation = Orientation.Vertical,
            Width = 500f,
            Height = 130f,
            Margin = new Thickness(24f, 22f, 24f, 22f),
        };
        pulseCard.Content = pulseStack;

        worldPulseText = CreateText("", 16f, theme.AccentGold);
        atmosphereText = CreateText("", 18f, theme.TextPrimary);
        omenText = CreateText("", 15f, theme.TextMuted);
        omenText.WrapText = true;
        omenText.Height = 40f;
        journeyText = CreateText("", 15f, theme.AccentCyan);
        journeyText.WrapText = true;
        pulseStack.Children.Add(worldPulseText);
        pulseStack.Children.Add(atmosphereText);
        pulseStack.Children.Add(omenText);
        pulseStack.Children.Add(journeyText);

        StackPanel optionsStack = new()
        {
            Orientation = Orientation.Vertical,
            Width = 760f,
            Height = 430f,
        };
        UIElementExtensions.SetCanvasPinOrigin(optionsStack, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(optionsStack, new Vector3(0.06f, 0.44f, 0f));
        root.Children.Add(optionsStack);

        optionWidgets = new[]
        {
            CreateOption("Continue Journey", "Enter the terrain runtime with the formal contextual HUD and world-law systems active."),
            CreateOption("Begin New Journey", "Launch a fresh world-state flow through the same terrain runtime entry point."),
            CreateOption("UI Showcase", "Open the dedicated interface presentation scene and inspect all visual states."),
            CreateOption("Legacy Prototype", "Open the earlier systemic prototype for comparison and regression checks."),
            CreateOption("Exit", "Close the application directly from the menu layer."),
        };

        foreach (MainMenuOptionWidgets option in optionWidgets)
        {
            optionsStack.Children.Add(option.Border);
        }

        footerHintText = CreateText("", 14f, theme.TextMuted);
        UIElementExtensions.SetCanvasPinOrigin(footerHintText, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(footerHintText, new Vector3(0.06f, 0.90f, 0f));
        root.Children.Add(footerHintText);

        return root;
    }

    private MainMenuOptionWidgets CreateOption(string title, string description)
    {
        Border border = new()
        {
            Width = 760f,
            Height = 72f,
            Margin = new Thickness(0f, 0f, 0f, 14f),
            BackgroundColor = theme.PanelBase,
            BorderColor = theme.PanelBorder,
            BorderThickness = new Thickness(2f, 2f, 2f, 2f),
        };

        StackPanel stack = new()
        {
            Orientation = Orientation.Vertical,
            Width = 700f,
            Height = 48f,
            Margin = new Thickness(22f, 10f, 22f, 10f),
        };

        TextBlock label = CreateText(title, 20f, theme.TextPrimary);
        TextBlock body = CreateText(description, 13f, theme.TextMuted);
        body.WrapText = true;
        stack.Children.Add(label);
        stack.Children.Add(body);
        border.Content = stack;
        return new MainMenuOptionWidgets(border, label, body);
    }

    private TextBlock CreateText(string text, float size, Color color)
    {
        return new TextBlock
        {
            Text = text,
            Font = font,
            TextSize = size,
            TextColor = color,
        };
    }

    private static void SetText(TextBlock? textBlock, string value)
    {
        if (textBlock is not null)
        {
            textBlock.Text = value;
        }
    }

    private sealed class MainMenuOptionWidgets
    {
        public MainMenuOptionWidgets(Border border, TextBlock label, TextBlock description)
        {
            Border = border;
            Label = label;
            Description = description;
        }

        public Border Border { get; }

        public TextBlock Label { get; }

        public TextBlock Description { get; }
    }
}
