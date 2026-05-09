#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace Game58date.Gameplay.UI;

public sealed class GameUiComposer
{
    private readonly GameUiTheme theme;

    private UIComponent? uiComponent;
    private Canvas? root;
    private TextBlock? titleText;
    private TextBlock? stageText;
    private TextBlock? biomeText;
    private TextBlock? omenText;
    private TextBlock? intentText;
    private TextBlock? profileText;
    private TextBlock? perceptionText;
    private TextBlock? helpText;
    private TextBlock? inputHintText;
    private TextBlock? lastIntentSummaryText;
    private TextBlock? narrativeReasonText;
    private TextBlock? historyTitleText;
    private TextBlock? historyEntryA;
    private TextBlock? historyEntryB;
    private TextBlock? historyEntryC;
    private Border? menuPanel;
    private TextBlock? menuTitleText;
    private TextBlock? saveSlotText;
    private TextBlock? saveMetaText;
    private TextBlock? settingsSummaryText;
    private TextBlock? menuHintText;
    private EditText? intentInput;
    private Border? intentInputBorder;
    private Button? menuToggleButton;
    private Button? focusIntentButton;
    private Button? toggleHudButton;
    private StackPanel? logPanel;
    private bool menuVisible;
    private WorldLawRuntimeController? worldLawController;

    public GameUiComposer(GameUiTheme theme)
    {
        this.theme = theme;
    }

    public void Attach(Entity owner, IGame game, WorldLawRuntimeController controller)
    {
        worldLawController = controller;
        uiComponent = owner.Get<UIComponent>();
        if (uiComponent is null)
        {
            uiComponent = new UIComponent();
            owner.Add(uiComponent);
        }

        root = BuildRoot();
        uiComponent.Page = new UIPage { RootElement = root };
        Update(controller.RuntimeState);
    }

    public void Update(WorldLawRuntimeState state)
    {
        if (root is null)
        {
            return;
        }

        if (titleText is not null)
        {
            titleText.Text = "GAME58DATE";
        }

        if (stageText is not null)
        {
            stageText.Text = $"Hero Journey: {WorldLawEngine.GetStageTitle(state.Narrative.CurrentStage)}";
        }

        if (biomeText is not null)
        {
            biomeText.Text = $"Target Biome: {state.World.TargetBiome}";
        }

        if (omenText is not null)
        {
            string source = state.Omen.LastSource switch
            {
                OmenSource.Intent => "Intent",
                OmenSource.EmergentWorldLaw => "World",
                OmenSource.Causality => "Causality",
                OmenSource.Narrative => "Narrative",
                _ => "Dormant",
            };
            omenText.Text = $"Omen: {WorldLawEngine.GetOmenTitle(state.Omen.ActiveOmen?.OmenType ?? state.World.LastOmen)}  Score {state.Omen.LastScore * 100f:0}%  Source {source}";
        }

        if (intentText is not null)
        {
            string topic = state.Intent.LastIntent?.Topic.ToString() ?? "Unknown";
            float confidence = state.Intent.LastIntent?.Confidence ?? 0f;
            intentText.Text = $"Intent: {topic}  Confidence {confidence * 100f:0}%  Total {state.Intent.SubmittedIntentCount}";
        }

        if (profileText is not null)
        {
            profileText.Text =
                $"Profile  Peace {state.Behavior.PeacefulTendency * 100f:0}%  Violence {state.Behavior.ViolentTendency * 100f:0}%  Faith {state.Behavior.FaithTendency * 100f:0}%  Curiosity {state.Behavior.CuriosityTendency * 100f:0}%";
        }

        if (perceptionText is not null)
        {
            string active = state.Perception.IsActive ? "Awakened" : "Dormant";
            perceptionText.Text =
                $"Perception: {active}  Power {state.Perception.Intensity * 100f:0}%  Remaining {state.Perception.ActiveSecondsRemaining:0.0}s  Cooldown {state.Perception.CooldownSecondsRemaining:0.0}s";
        }

        if (helpText is not null)
        {
            helpText.Text = "Enter submit  Tab focus  Q sense  F2 sea  F3 loss  F4 violent  F5 peaceful  F6 mentor";
        }

        if (inputHintText is not null)
        {
            inputHintText.Text = state.Intent.TextInputEnabled
                ? "Your next sentence can bend the world. Speak a destination, desire, warning, or vow."
                : "Text focus is currently disabled. Press Tab to return to narrative input mode.";
        }

        if (lastIntentSummaryText is not null)
        {
            string summary = state.Intent.LastIntent?.Summary ?? "No structured intent has been submitted yet.";
            lastIntentSummaryText.Text = summary;
        }

        if (narrativeReasonText is not null)
        {
            narrativeReasonText.Text = state.Narrative.LastStageReason;
        }

        if (intentInput is not null)
        {
            string text = state.Intent.LastIntent?.RawText ?? string.Empty;
            if (!string.Equals(intentInput.Text, text, StringComparison.Ordinal))
            {
                intentInput.Text = text;
            }
            intentInput.IsReadOnly = !state.Intent.TextInputEnabled;
        }

        if (intentInputBorder is not null)
        {
            intentInputBorder.BorderColor = state.Perception.IsActive ? theme.AccentCyan : theme.PanelBorder;
            intentInputBorder.BackgroundColor = state.Perception.IsActive ? theme.PanelElevated : theme.InputFill;
        }

        if (historyTitleText is not null)
        {
            historyTitleText.Text = "Recent Signals";
        }

        if (historyEntryA is not null)
        {
            historyEntryA.Text = FormatHistoryLine(state, 0);
        }

        if (historyEntryB is not null)
        {
            historyEntryB.Text = FormatHistoryLine(state, 1);
        }

        if (historyEntryC is not null)
        {
            historyEntryC.Text = FormatHistoryLine(state, 2);
        }

        if (menuPanel is not null)
        {
            menuPanel.Visibility = menuVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        if (menuTitleText is not null)
        {
            menuTitleText.Text = "System Atlas";
        }

        if (saveSlotText is not null)
        {
            saveSlotText.Text = $"Current Stage: {WorldLawEngine.GetStageTitle(state.Narrative.CurrentStage)}";
        }

        if (saveMetaText is not null)
        {
            saveMetaText.Text =
                $"Signals {state.Omen.History.Count}  Intents {state.Intent.SubmittedIntentCount}  Perception Uses {state.Perception.ActivationCount}";
        }

        if (settingsSummaryText is not null)
        {
            settingsSummaryText.Text =
                $"Visual Mode: Ritual Flat  |  Pattern Veil: On  |  Perception Boost: {(state.Perception.IsActive ? "Active" : "Passive")}  |  Input Focus: {(state.Intent.TextInputEnabled ? "Narrative" : "Travel")}";
        }

        if (menuHintText is not null)
        {
            menuHintText.Text = "Esc / F10 opens the atlas. Use it as the game's systemic pause and reference layer.";
        }
    }

    private Canvas BuildRoot()
    {
        var canvas = new Canvas
        {
            Width = 1920f,
            Height = 1080f,
        };

        Border overlay = CreateBorder(theme.BackgroundVeil, new Thickness(0f, 0f, 0f, 0f), 1920f, 1080f);
        UIElementExtensions.SetCanvasRelativePosition(overlay, new Vector3(0f, 0f, 0f));
        canvas.Children.Add(overlay);

        Border patternBand = CreateBorder(theme.BackgroundPattern, new Thickness(1f, 1f, 1f, 1f), 1920f, 180f);
        UIElementExtensions.SetCanvasRelativePosition(patternBand, new Vector3(0f, 0f, 0f));
        canvas.Children.Add(patternBand);

        Border headerPanel = CreateBorder(theme.PanelBase, new Thickness(2f, 2f, 2f, 2f), 780f, 176f);
        UIElementExtensions.SetCanvasRelativePosition(headerPanel, new Vector3(0.04f, 0.04f, 0f));
        canvas.Children.Add(headerPanel);

        StackPanel headerStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 720f,
            Height = 152f,
            Margin = new Thickness(24f, 16f, 24f, 16f),
        };
        headerPanel.Content = headerStack;

        titleText = CreateText("", 40f, theme.AccentGold);
        stageText = CreateText("", 18f, theme.TextPrimary);
        biomeText = CreateText("", 16f, theme.TextMuted);
        omenText = CreateText("", 15f, theme.TextPrimary);
        headerStack.Children.Add(titleText);
        headerStack.Children.Add(stageText);
        headerStack.Children.Add(biomeText);
        headerStack.Children.Add(omenText);

        Border rightPanel = CreateBorder(theme.PanelBase, new Thickness(2f, 2f, 2f, 2f), 540f, 196f);
        UIElementExtensions.SetCanvasRelativePosition(rightPanel, new Vector3(0.68f, 0.04f, 0f));
        canvas.Children.Add(rightPanel);

        StackPanel rightStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 492f,
            Height = 164f,
            Margin = new Thickness(24f, 18f, 24f, 14f),
        };
        rightPanel.Content = rightStack;

        profileText = CreateText("", 15f, theme.TextPrimary);
        perceptionText = CreateText("", 15f, theme.AccentCyan);
        intentText = CreateText("", 15f, theme.TextMuted);
        rightStack.Children.Add(profileText);
        rightStack.Children.Add(perceptionText);
        rightStack.Children.Add(intentText);

        Border bottomPanel = CreateBorder(theme.PanelElevated, new Thickness(2f, 2f, 2f, 2f), 1320f, 210f);
        UIElementExtensions.SetCanvasRelativePosition(bottomPanel, new Vector3(0.04f, 0.78f, 0f));
        canvas.Children.Add(bottomPanel);

        Grid bottomGrid = new Grid
        {
            Width = 1260f,
            Height = 170f,
            Margin = new Thickness(28f, 20f, 32f, 20f),
        };
        bottomGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 0.72f));
        bottomGrid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 0.28f));
        bottomPanel.Content = bottomGrid;

        StackPanel inputStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 860f,
            Height = 164f,
        };
        inputStack.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
        bottomGrid.Children.Add(inputStack);

        helpText = CreateText("", 14f, theme.TextMuted);
        inputStack.Children.Add(helpText);

        inputHintText = CreateText("", 15f, theme.TextPrimary);
        inputHintText.WrapText = true;
        inputHintText.Height = 40f;
        inputStack.Children.Add(inputHintText);

        intentInputBorder = CreateBorder(theme.InputFill, new Thickness(2f, 2f, 2f, 2f), 860f, 66f);
        inputStack.Children.Add(intentInputBorder);

        intentInput = new EditText
        {
            Width = 820f,
            Height = 54f,
            Margin = new Thickness(18f, 6f, 18f, 6f),
            Text = string.Empty,
            TextColor = theme.TextPrimary,
            SelectionColor = theme.AccentGold,
            CaretColor = theme.AccentCyan,
            TextSize = 18f,
        };
        intentInputBorder.Content = intentInput;

        lastIntentSummaryText = CreateText("", 14f, theme.TextMuted);
        lastIntentSummaryText.WrapText = true;
        lastIntentSummaryText.Height = 36f;
        inputStack.Children.Add(lastIntentSummaryText);

        Border sideInfoPanel = CreateBorder(theme.PanelBase, new Thickness(2f, 2f, 2f, 2f), 340f, 164f);
        sideInfoPanel.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
        bottomGrid.Children.Add(sideInfoPanel);

        logPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 300f,
            Height = 132f,
            Margin = new Thickness(18f, 16f, 18f, 16f),
        };
        sideInfoPanel.Content = logPanel;

        historyTitleText = CreateText("Recent Signals", 16f, theme.AccentGold);
        historyEntryA = CreateText("", 14f, theme.TextPrimary);
        historyEntryA.WrapText = true;
        historyEntryA.Height = 34f;
        historyEntryB = CreateText("", 13f, theme.TextMuted);
        historyEntryB.WrapText = true;
        historyEntryB.Height = 28f;
        historyEntryC = CreateText("", 13f, theme.TextMuted);
        historyEntryC.WrapText = true;
        historyEntryC.Height = 28f;

        logPanel.Children.Add(historyTitleText);
        logPanel.Children.Add(historyEntryA);
        logPanel.Children.Add(historyEntryB);
        logPanel.Children.Add(historyEntryC);

        Border narrativePanel = CreateBorder(theme.PanelBase, new Thickness(2f, 2f, 2f, 2f), 1320f, 112f);
        UIElementExtensions.SetCanvasRelativePosition(narrativePanel, new Vector3(0.04f, 0.64f, 0f));
        canvas.Children.Add(narrativePanel);

        StackPanel narrativeStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 1260f,
            Height = 80f,
            Margin = new Thickness(28f, 16f, 28f, 16f),
        };
        narrativePanel.Content = narrativeStack;

        narrativeStack.Children.Add(CreateText("Journey Logic", 15f, theme.AccentGold));
        narrativeReasonText = CreateText("", 14f, theme.TextMuted);
        narrativeReasonText.WrapText = true;
        narrativeStack.Children.Add(narrativeReasonText);

        menuPanel = CreateBorder(theme.PanelElevated, new Thickness(2f, 2f, 2f, 2f), 620f, 420f);
        UIElementExtensions.SetCanvasRelativePosition(menuPanel, new Vector3(0.50f, 0.18f, 0f));
        menuPanel.Visibility = Visibility.Collapsed;
        canvas.Children.Add(menuPanel);

        StackPanel menuStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 560f,
            Height = 372f,
            Margin = new Thickness(28f, 22f, 28f, 22f),
        };
        menuPanel.Content = menuStack;

        menuTitleText = CreateText("System Atlas", 26f, theme.AccentGold);
        saveSlotText = CreateText("", 16f, theme.TextPrimary);
        saveMetaText = CreateText("", 14f, theme.TextMuted);
        settingsSummaryText = CreateText("", 14f, theme.TextPrimary);
        settingsSummaryText.WrapText = true;
        menuHintText = CreateText("", 13f, theme.TextMuted);
        menuHintText.WrapText = true;

        menuStack.Children.Add(menuTitleText);
        menuStack.Children.Add(saveSlotText);
        menuStack.Children.Add(saveMetaText);
        menuStack.Children.Add(settingsSummaryText);
        menuStack.Children.Add(menuHintText);

        StackPanel buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Width = 560f,
            Height = 72f,
            Margin = new Thickness(0f, 18f, 0f, 0f),
        };
        menuStack.Children.Add(buttonRow);

        menuToggleButton = CreateButton("Toggle Atlas");
        focusIntentButton = CreateButton("Intent Focus");
        toggleHudButton = CreateButton("Debug HUD");
        menuToggleButton.Click += HandleMenuToggleClicked;
        focusIntentButton.Click += HandleIntentFocusClicked;
        toggleHudButton.Click += HandleToggleHudClicked;
        buttonRow.Children.Add(menuToggleButton);
        buttonRow.Children.Add(focusIntentButton);
        buttonRow.Children.Add(toggleHudButton);

        return canvas;
    }

    private Border CreateBorder(Color fill, Thickness borderThickness, float width, float height)
    {
        return new Border
        {
            Width = width,
            Height = height,
            BackgroundColor = fill,
            BorderColor = theme.PanelBorder,
            BorderThickness = borderThickness,
            Padding = new Thickness(0f, 0f, 0f, 0f),
        };
    }

    private TextBlock CreateText(string text, float size, Color color)
    {
        return new TextBlock
        {
            Text = text,
            TextColor = color,
            TextSize = size,
        };
    }

    private Button CreateButton(string label)
    {
        return new Button
        {
            Width = 170f,
            Height = 52f,
            Margin = new Thickness(0f, 0f, 14f, 0f),
            Content = CreateText(label, 15f, theme.TextPrimary),
            ClickMode = ClickMode.Release,
            Color = theme.PanelOrnament,
        };
    }

    public void ToggleMenu()
    {
        menuVisible = !menuVisible;
        if (menuPanel is not null)
        {
            menuPanel.Visibility = menuVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void HandleMenuToggleClicked(object? sender, RoutedEventArgs e)
    {
        ToggleMenu();
    }

    private void HandleIntentFocusClicked(object? sender, RoutedEventArgs e)
    {
        if (worldLawController is null)
        {
            return;
        }

        bool next = !worldLawController.RuntimeState.Intent.TextInputEnabled;
        worldLawController.SetIntentTextInputEnabled(next);
    }

    private void HandleToggleHudClicked(object? sender, RoutedEventArgs e)
    {
        if (worldLawController is null)
        {
            return;
        }

        worldLawController.SetDebugHudVisible(!worldLawController.IsDebugHudVisible);
    }

    private static string FormatHistoryLine(WorldLawRuntimeState state, int index)
    {
        if (state.Omen.History.Count <= index)
        {
            return index == 0
                ? "No major omen has resolved into visible history yet."
                : string.Empty;
        }

        OmenRecord omen = state.Omen.History[state.Omen.History.Count - 1 - index];
        return $"{WorldLawEngine.GetOmenTitle(omen.OmenType)}  {omen.Score * 100f:0}%  {omen.Source}";
    }
}
