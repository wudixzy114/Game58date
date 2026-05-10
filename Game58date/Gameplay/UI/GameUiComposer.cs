#nullable enable
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace Game58date.Gameplay.UI;

public sealed class GameUiComposer
{
    private readonly GameUiTheme theme;
    private SpriteFont? font;

    private UIComponent? uiComponent;
    private Canvas? root;
    private Border? modeTagBorder;
    private TextBlock? modeTagText;
    private TextBlock? titleText;
    private TextBlock? subtitleText;
    private TextBlock? stageText;
    private TextBlock? biomeText;
    private TextBlock? modeSummaryText;
    private Border? omenCalloutBorder;
    private TextBlock? omenText;
    private TextBlock? omenDetailText;
    private TextBlock? profileText;
    private TextBlock? perceptionText;
    private TextBlock? intentText;
    private TextBlock? worldPulseSummaryText;
    private TextBlock? helpText;
    private TextBlock? inputHintText;
    private EditText? intentInput;
    private Border? intentInputBorder;
    private TextBlock? lastIntentSummaryText;
    private TextBlock? narrativeTitleText;
    private TextBlock? narrativeReasonText;
    private TextBlock? historyTitleText;
    private TextBlock? historyEntryA;
    private TextBlock? historyEntryB;
    private TextBlock? historyEntryC;
    private Border? menuPanel;
    private TextBlock? menuTitleText;
    private TextBlock? menuStageText;
    private TextBlock? menuMetaText;
    private TextBlock? menuSettingsText;
    private TextBlock? menuHintText;
    private Button? menuToggleButton;
    private Button? narrativeInputButton;
    private Button? debugHudButton;
    private GameUiMeterWidgets? karmaMeter;
    private GameUiMeterWidgets? blessingMeter;
    private GameUiMeterWidgets? pathMeter;
    private GameUiMeterWidgets? dangerMeter;
    private IGameUiCommandSink? commandSink;

    public GameUiComposer(GameUiTheme theme)
    {
        this.theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    public void Attach(Entity owner, IServiceRegistry services, IGameUiCommandSink sink)
    {
        commandSink = sink ?? throw new ArgumentNullException(nameof(sink));
        uiComponent = owner.Get<UIComponent>();
        if (uiComponent is null)
        {
            uiComponent = new UIComponent
            {
                Resolution = new Vector3(1920f, 1080f, 1000f),
            };
            owner.Add(uiComponent);
        }

        font = GameUiFontProvider.Load(services);
        root = BuildRoot();
        uiComponent.Page = new UIPage { RootElement = root };
    }

    public void Update(GameUiViewState viewState)
    {
        if (root is null)
        {
            return;
        }

        SetText(modeTagText, viewState.ModeTagText);
        SetText(titleText, viewState.TitleText);
        SetText(subtitleText, viewState.SubtitleText);
        SetText(stageText, viewState.StageText);
        SetText(biomeText, viewState.BiomeText);
        SetText(modeSummaryText, viewState.ModeSummaryText);
        SetText(omenText, viewState.OmenText);
        SetText(omenDetailText, viewState.OmenDetailText);
        SetText(profileText, viewState.ProfileText);
        SetText(perceptionText, viewState.PerceptionText);
        SetText(intentText, viewState.IntentText);
        SetText(worldPulseSummaryText, viewState.WorldPulseSummaryText);
        SetText(helpText, viewState.HelpText);
        SetText(inputHintText, viewState.InputHintText);
        SetText(lastIntentSummaryText, viewState.LastIntentSummaryText);
        SetText(narrativeTitleText, viewState.NarrativeTitleText);
        SetText(narrativeReasonText, viewState.NarrativeReasonText);
        UpdateSignalRail(viewState);
        SetText(menuTitleText, viewState.MenuTitleText);
        SetText(menuStageText, viewState.MenuStageText);
        SetText(menuMetaText, viewState.MenuMetaText);
        SetText(menuSettingsText, viewState.MenuSettingsText);
        SetText(menuHintText, viewState.MenuHintText);
        SetButtonText(menuToggleButton, viewState.MenuToggleButtonText);
        SetButtonText(narrativeInputButton, viewState.NarrativeInputButtonText);
        SetButtonText(debugHudButton, viewState.DebugHudButtonText);

        if (intentInput is not null)
        {
            if (!string.Equals(intentInput.Text, viewState.IntentDraftText, StringComparison.Ordinal))
            {
                intentInput.Text = viewState.IntentDraftText;
            }

            intentInput.IsReadOnly = true;
        }

        if (modeTagBorder is not null)
        {
            modeTagBorder.BackgroundColor = viewState.ModeTagFillColor;
        }

        if (modeTagText is not null)
        {
            modeTagText.TextColor = viewState.ModeTagTextColor;
        }

        if (omenCalloutBorder is not null)
        {
            omenCalloutBorder.BorderColor = viewState.OmenAccentColor;
        }

        if (intentInputBorder is not null)
        {
            intentInputBorder.BorderColor = viewState.InputBorderColor;
            intentInputBorder.BackgroundColor = viewState.InputFillColor;
        }

        if (menuPanel is not null)
        {
            menuPanel.Visibility = viewState.MenuVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        UpdateMeter(karmaMeter, viewState.KarmaMeter);
        UpdateMeter(blessingMeter, viewState.BlessingMeter);
        UpdateMeter(pathMeter, viewState.PathMeter);
        UpdateMeter(dangerMeter, viewState.DangerMeter);
    }

    private Canvas BuildRoot()
    {
        var canvas = new Canvas
        {
            Width = 1920f,
            Height = 1080f,
        };

        Border veil = CreateBorder(theme.BackgroundVeil, theme.PanelBorder, new Thickness(0f, 0f, 0f, 0f), 1920f, 1080f);
        UIElementExtensions.SetCanvasPinOrigin(veil, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(veil, new Vector3(0f, 0f, 0f));
        canvas.Children.Add(veil);

        Border topBand = CreateBorder(theme.BackgroundPattern, theme.PanelBorder, new Thickness(0f, 0f, 0f, 0f), 1920f, 190f);
        UIElementExtensions.SetCanvasPinOrigin(topBand, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(topBand, new Vector3(0f, 0f, 0f));
        canvas.Children.Add(topBand);

        Border headerPanel = CreateBorder(theme.PanelBase, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 840f, 234f);
        UIElementExtensions.SetCanvasPinOrigin(headerPanel, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(headerPanel, new Vector3(0.04f, 0.05f, 0f));
        canvas.Children.Add(headerPanel);

        var headerStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 780f,
            Height = 188f,
            Margin = new Thickness(28f, 20f, 28f, 20f),
        };
        headerPanel.Content = headerStack;

        modeTagBorder = CreateBorder(theme.AccentGold, theme.AccentGold, new Thickness(0f, 0f, 0f, 0f), 170f, 34f);
        modeTagText = CreateText("", 15f, theme.PanelBase);
        modeTagText.HorizontalAlignment = HorizontalAlignment.Center;
        modeTagText.VerticalAlignment = VerticalAlignment.Center;
        modeTagBorder.Content = modeTagText;
        headerStack.Children.Add(modeTagBorder);

        titleText = CreateText("", 42f, theme.TextPrimary);
        subtitleText = CreateText("", 15f, theme.TextMuted);
        subtitleText.WrapText = true;
        subtitleText.Height = 42f;
        stageText = CreateText("", 19f, theme.TextPrimary);
        biomeText = CreateText("", 16f, theme.TextMuted);
        modeSummaryText = CreateText("", 15f, theme.AccentCyan);
        headerStack.Children.Add(titleText);
        headerStack.Children.Add(subtitleText);
        headerStack.Children.Add(stageText);
        headerStack.Children.Add(biomeText);
        headerStack.Children.Add(modeSummaryText);

        omenCalloutBorder = CreateBorder(theme.PanelElevated, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 620f, 144f);
        UIElementExtensions.SetCanvasPinOrigin(omenCalloutBorder, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(omenCalloutBorder, new Vector3(0.66f, 0.05f, 0f));
        canvas.Children.Add(omenCalloutBorder);

        var omenStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 568f,
            Height = 100f,
            Margin = new Thickness(24f, 20f, 24f, 20f),
        };
        omenCalloutBorder.Content = omenStack;

        omenStack.Children.Add(CreateText("Dominant Omen", 14f, theme.AccentGold));
        omenText = CreateText("", 18f, theme.TextPrimary);
        omenDetailText = CreateText("", 14f, theme.TextMuted);
        omenDetailText.WrapText = true;
        omenDetailText.Height = 44f;
        omenStack.Children.Add(omenText);
        omenStack.Children.Add(omenDetailText);

        Border profilePanel = CreateBorder(theme.PanelBase, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 620f, 160f);
        UIElementExtensions.SetCanvasPinOrigin(profilePanel, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(profilePanel, new Vector3(0.66f, 0.21f, 0f));
        canvas.Children.Add(profilePanel);

        var profileStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 568f,
            Height = 116f,
            Margin = new Thickness(24f, 20f, 24f, 20f),
        };
        profilePanel.Content = profileStack;

        profileText = CreateText("", 14f, theme.TextPrimary);
        profileText.WrapText = true;
        profileText.Height = 40f;
        perceptionText = CreateText("", 15f, theme.AccentCyan);
        intentText = CreateText("", 14f, theme.TextMuted);
        worldPulseSummaryText = CreateText("", 14f, theme.AccentGold);
        worldPulseSummaryText.WrapText = true;
        worldPulseSummaryText.Height = 34f;
        profileStack.Children.Add(profileText);
        profileStack.Children.Add(perceptionText);
        profileStack.Children.Add(intentText);
        profileStack.Children.Add(worldPulseSummaryText);

        Border metersPanel = CreateBorder(theme.PanelBase, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 840f, 194f);
        UIElementExtensions.SetCanvasPinOrigin(metersPanel, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(metersPanel, new Vector3(0.04f, 0.28f, 0f));
        canvas.Children.Add(metersPanel);

        Grid meterGrid = new Grid
        {
            Width = 780f,
            Height = 150f,
            Margin = new Thickness(28f, 22f, 28f, 22f),
        };

        meterGrid.RowDefinitions.Add(new StripDefinition(StripType.Star, 1f));
        meterGrid.RowDefinitions.Add(new StripDefinition(StripType.Star, 1f));
        meterGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 1f));
        meterGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 1f));
        metersPanel.Content = meterGrid;

        karmaMeter = CreateMeterCard(meterGrid, 0, 0);
        blessingMeter = CreateMeterCard(meterGrid, 0, 1);
        pathMeter = CreateMeterCard(meterGrid, 1, 0);
        dangerMeter = CreateMeterCard(meterGrid, 1, 1);

        Border narrativePanel = CreateBorder(theme.PanelBase, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 840f, 128f);
        UIElementExtensions.SetCanvasPinOrigin(narrativePanel, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(narrativePanel, new Vector3(0.04f, 0.48f, 0f));
        canvas.Children.Add(narrativePanel);

        var narrativeStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 780f,
            Height = 84f,
            Margin = new Thickness(28f, 18f, 28f, 18f),
        };
        narrativePanel.Content = narrativeStack;

        narrativeTitleText = CreateText("Journey Logic", 15f, theme.AccentGold);
        narrativeReasonText = CreateText("", 14f, theme.TextMuted);
        narrativeReasonText.WrapText = true;
        narrativeReasonText.Height = 52f;
        narrativeStack.Children.Add(narrativeTitleText);
        narrativeStack.Children.Add(narrativeReasonText);

        Border bottomPanel = CreateBorder(theme.PanelElevated, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 1320f, 256f);
        UIElementExtensions.SetCanvasPinOrigin(bottomPanel, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(bottomPanel, new Vector3(0.04f, 0.70f, 0f));
        canvas.Children.Add(bottomPanel);

        Grid bottomGrid = new Grid
        {
            Width = 1260f,
            Height = 208f,
            Margin = new Thickness(28f, 24f, 32f, 24f),
        };
        bottomGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 0.73f));
        bottomGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 0.27f));
        bottomPanel.Content = bottomGrid;

        var inputStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 880f,
            Height = 198f,
        };
        inputStack.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
        bottomGrid.Children.Add(inputStack);

        helpText = CreateText("", 14f, theme.TextMuted);
        inputHintText = CreateText("", 15f, theme.TextPrimary);
        inputHintText.WrapText = true;
        inputHintText.Height = 42f;
        intentInputBorder = CreateBorder(theme.InputFill, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 880f, 72f);
        intentInput = new EditText
        {
            Width = 836f,
            Height = 56f,
            Margin = new Thickness(22f, 8f, 22f, 8f),
            Text = string.Empty,
            TextColor = theme.TextPrimary,
            SelectionColor = theme.AccentGold,
            CaretColor = theme.AccentCyan,
            TextSize = 19f,
            IsReadOnly = true,
        };
        intentInputBorder.Content = intentInput;
        lastIntentSummaryText = CreateText("", 14f, theme.TextMuted);
        lastIntentSummaryText.WrapText = true;
        lastIntentSummaryText.Height = 44f;

        inputStack.Children.Add(helpText);
        inputStack.Children.Add(inputHintText);
        inputStack.Children.Add(intentInputBorder);
        inputStack.Children.Add(lastIntentSummaryText);

        Border historyPanel = CreateBorder(theme.PanelBase, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 322f, 208f);
        historyPanel.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
        bottomGrid.Children.Add(historyPanel);

        var historyStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 278f,
            Height = 164f,
            Margin = new Thickness(22f, 20f, 22f, 20f),
        };
        historyPanel.Content = historyStack;

        historyTitleText = CreateText("Recent Signals", 16f, theme.AccentGold);
        historyEntryA = CreateText("", 14f, theme.TextPrimary);
        historyEntryA.WrapText = true;
        historyEntryA.Height = 40f;
        historyEntryB = CreateText("", 13f, theme.TextMuted);
        historyEntryB.WrapText = true;
        historyEntryB.Height = 34f;
        historyEntryC = CreateText("", 13f, theme.TextMuted);
        historyEntryC.WrapText = true;
        historyEntryC.Height = 34f;

        historyStack.Children.Add(historyTitleText);
        historyStack.Children.Add(historyEntryA);
        historyStack.Children.Add(historyEntryB);
        historyStack.Children.Add(historyEntryC);

        menuPanel = CreateBorder(theme.PanelElevated, theme.PanelBorder, new Thickness(2f, 2f, 2f, 2f), 664f, 468f);
        UIElementExtensions.SetCanvasPinOrigin(menuPanel, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(menuPanel, new Vector3(0.48f, 0.17f, 0f));
        menuPanel.Visibility = Visibility.Collapsed;
        canvas.Children.Add(menuPanel);

        var menuStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 604f,
            Height = 418f,
            Margin = new Thickness(30f, 24f, 30f, 24f),
        };
        menuPanel.Content = menuStack;

        menuTitleText = CreateText("System Atlas", 28f, theme.AccentGold);
        menuStageText = CreateText("", 16f, theme.TextPrimary);
        menuMetaText = CreateText("", 14f, theme.TextMuted);
        menuSettingsText = CreateText("", 14f, theme.TextPrimary);
        menuSettingsText.WrapText = true;
        menuSettingsText.Height = 44f;
        menuHintText = CreateText("", 13f, theme.TextMuted);
        menuHintText.WrapText = true;
        menuHintText.Height = 38f;

        menuStack.Children.Add(menuTitleText);
        menuStack.Children.Add(menuStageText);
        menuStack.Children.Add(menuMetaText);
        menuStack.Children.Add(menuSettingsText);
        menuStack.Children.Add(menuHintText);

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Width = 604f,
            Height = 56f,
            Margin = new Thickness(0f, 24f, 0f, 0f),
        };
        menuStack.Children.Add(buttonRow);

        menuToggleButton = CreateButton("Open Atlas");
        narrativeInputButton = CreateButton("Enable Input");
        debugHudButton = CreateButton("Show Debug HUD");
        menuToggleButton.Click += HandleMenuToggleClicked;
        narrativeInputButton.Click += HandleNarrativeInputClicked;
        debugHudButton.Click += HandleDebugHudClicked;
        buttonRow.Children.Add(menuToggleButton);
        buttonRow.Children.Add(narrativeInputButton);
        buttonRow.Children.Add(debugHudButton);

        return canvas;
    }

    private GameUiMeterWidgets CreateMeterCard(Grid parent, int row, int column)
    {
        Border card = CreateBorder(theme.PanelOrnament, theme.PanelBorder, new Thickness(1f, 1f, 1f, 1f), 374f, 64f);
        card.DependencyProperties.Set(GridBase.RowPropertyKey, row);
        card.DependencyProperties.Set(GridBase.ColumnPropertyKey, column);
        parent.Children.Add(card);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 330f,
            Height = 48f,
            Margin = new Thickness(18f, 8f, 18f, 8f),
        };
        card.Content = stack;

        Grid lineGrid = new Grid
        {
            Width = 330f,
            Height = 20f,
        };
        lineGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 0.7f));
        lineGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 0.3f));
        stack.Children.Add(lineGrid);

        TextBlock label = CreateText("", 14f, theme.TextPrimary);
        TextBlock value = CreateText("", 13f, theme.TextMuted);
        value.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
        lineGrid.Children.Add(label);
        lineGrid.Children.Add(value);

        Grid meterTrack = new Grid
        {
            Width = 330f,
            Height = 10f,
            BackgroundColor = theme.InputFill,
            Margin = new Thickness(0f, 6f, 0f, 6f),
        };
        Border meterFill = new Border
        {
            Width = 0f,
            Height = 10f,
            BackgroundColor = theme.AccentGold,
            BorderThickness = new Thickness(0f, 0f, 0f, 0f),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        meterTrack.Children.Add(meterFill);
        stack.Children.Add(meterTrack);

        TextBlock summary = CreateText("", 12f, theme.TextMuted);
        stack.Children.Add(summary);

        return new GameUiMeterWidgets(label, value, summary, meterFill, 330f);
    }

    private Border CreateBorder(Color fill, Color borderColor, Thickness borderThickness, float width, float height)
    {
        return new Border
        {
            Width = width,
            Height = height,
            BackgroundColor = fill,
            BorderColor = borderColor,
            BorderThickness = borderThickness,
        };
    }

    private TextBlock CreateText(string text, float size, Color color)
    {
        return new TextBlock
        {
            Text = text,
            Font = font,
            TextColor = color,
            TextSize = size,
        };
    }

    private Button CreateButton(string label)
    {
        return new Button
        {
            Width = 182f,
            Height = 52f,
            Margin = new Thickness(0f, 0f, 14f, 0f),
            Content = CreateText(label, 15f, theme.TextPrimary),
            ClickMode = ClickMode.Release,
            Color = theme.PanelOrnament,
        };
    }

    private static void SetText(TextBlock? textBlock, string value)
    {
        if (textBlock is not null)
        {
            textBlock.Text = value;
        }
    }

    private static void SetButtonText(Button? button, string value)
    {
        if (button?.Content is TextBlock textBlock)
        {
            textBlock.Text = value;
        }
    }

    private static void UpdateMeter(GameUiMeterWidgets? widgets, GameUiMeterViewState value)
    {
        if (widgets is null)
        {
            return;
        }

        widgets.Label.Text = value.LabelText;
        widgets.Value.Text = value.ValueText;
        widgets.Summary.Text = value.SummaryText;
        widgets.Fill.BackgroundColor = value.FillColor;
        widgets.Fill.Width = widgets.TrackWidth * MathUtil.Clamp(value.FillRatio, 0f, 1f);
    }

    private void UpdateSignalRail(GameUiViewState viewState)
    {
        if (viewState.Notices.Length > 0)
        {
            SetText(historyTitleText, "Signal Rail");
            SetText(historyEntryA, FormatNotice(viewState.Notices, 0));
            SetText(historyEntryB, FormatNotice(viewState.Notices, 1));
            SetText(historyEntryC, viewState.HistoryLines.Length > 0 ? viewState.HistoryLines[0] : string.Empty);
            return;
        }

        SetText(historyTitleText, viewState.HistoryTitleText);
        SetText(historyEntryA, viewState.HistoryLines.Length > 0 ? viewState.HistoryLines[0] : string.Empty);
        SetText(historyEntryB, viewState.HistoryLines.Length > 1 ? viewState.HistoryLines[1] : string.Empty);
        SetText(historyEntryC, viewState.HistoryLines.Length > 2 ? viewState.HistoryLines[2] : string.Empty);
    }

    private static string FormatNotice(GameUiNoticeRecord[] notices, int index)
    {
        if (notices.Length <= index)
        {
            return string.Empty;
        }

        GameUiNoticeRecord notice = notices[index];
        return $"{notice.TitleText}  {notice.BodyText}";
    }

    private void HandleMenuToggleClicked(object? sender, RoutedEventArgs e)
    {
        commandSink?.ToggleUiMenu();
    }

    private void HandleNarrativeInputClicked(object? sender, RoutedEventArgs e)
    {
        commandSink?.ToggleNarrativeInput();
    }

    private void HandleDebugHudClicked(object? sender, RoutedEventArgs e)
    {
        commandSink?.ToggleDebugHud();
    }

    private sealed class GameUiMeterWidgets
    {
        public GameUiMeterWidgets(TextBlock label, TextBlock value, TextBlock summary, Border fill, float trackWidth)
        {
            Label = label;
            Value = value;
            Summary = summary;
            Fill = fill;
            TrackWidth = trackWidth;
        }

        public TextBlock Label { get; }

        public TextBlock Value { get; }

        public TextBlock Summary { get; }

        public Border Fill { get; }

        public float TrackWidth { get; }
    }
}
