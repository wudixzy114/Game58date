#nullable enable
using System;
using Game58date.Gameplay.UI;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Game58date.Gameplay;

public sealed class SceneLoadingRuntimeScript : SyncScript
{
    private readonly GameUiTheme theme = GameUiTheme.Default;
    private UIComponent? uiComponent;
    private TextBlock? titleText;
    private TextBlock? detailText;
    private TextBlock? statusText;
    private float elapsedSeconds;
    private bool hasCommitted;

    public override void Start()
    {
        Game.IsMouseVisible = true;
        EnsureUi();
        UpdateTexts();
    }

    public override void Update()
    {
        elapsedSeconds += (float)Game.UpdateTime.Elapsed.TotalSeconds;
        UpdateTexts();

        if (hasCommitted || elapsedSeconds < 0.12f)
        {
            return;
        }

        hasCommitted = true;
        RuntimeSceneLauncher.CommitPendingLaunch(Services, SceneSystem);
    }

    private void EnsureUi()
    {
        uiComponent = Entity.Get<UIComponent>();
        if (uiComponent is null)
        {
            uiComponent = new UIComponent
            {
                Resolution = new Vector3(1920f, 1080f, 1000f),
            };
            Entity.Add(uiComponent);
        }

        var font = GameUiFontProvider.Load(Services);
        var root = new Canvas
        {
            Width = 1920f,
            Height = 1080f,
        };

        var veil = new Border
        {
            Width = 1920f,
            Height = 1080f,
            BackgroundColor = new Color(0.03f, 0.04f, 0.06f, 1f),
            BorderThickness = new Thickness(0f, 0f, 0f, 0f),
        };
        UIElementExtensions.SetCanvasPinOrigin(veil, new Vector3(0f, 0f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(veil, new Vector3(0f, 0f, 0f));
        root.Children.Add(veil);

        var frame = new Border
        {
            Width = 820f,
            Height = 280f,
            BackgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f),
            BorderColor = theme.PanelBorder,
            BorderThickness = new Thickness(2f, 2f, 2f, 2f),
        };
        UIElementExtensions.SetCanvasPinOrigin(frame, new Vector3(0.5f, 0.5f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(frame, new Vector3(0.5f, 0.5f, 0f));
        root.Children.Add(frame);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 720f,
            Height = 190f,
            Margin = new Thickness(50f, 42f, 50f, 42f),
        };
        frame.Content = stack;

        stack.Children.Add(new TextBlock
        {
            Text = "GAME58DATE",
            Font = font,
            TextSize = 20f,
            TextColor = theme.AccentGold,
        });

        titleText = new TextBlock
        {
            Font = font,
            TextSize = 34f,
            TextColor = theme.TextPrimary,
            Margin = new Thickness(0f, 18f, 0f, 0f),
        };
        detailText = new TextBlock
        {
            Font = font,
            TextSize = 16f,
            TextColor = theme.TextMuted,
            WrapText = true,
            Height = 60f,
            Margin = new Thickness(0f, 16f, 0f, 0f),
        };
        statusText = new TextBlock
        {
            Font = font,
            TextSize = 16f,
            TextColor = theme.AccentCyan,
            Margin = new Thickness(0f, 24f, 0f, 0f),
        };

        stack.Children.Add(titleText);
        stack.Children.Add(detailText);
        stack.Children.Add(statusText);

        uiComponent.Page = new UIPage
        {
            RootElement = root,
        };
    }

    private void UpdateTexts()
    {
        RuntimeLaunchRequest request = RuntimeSceneLauncher.PeekPendingLaunch()
            ?? RuntimeLaunchRequest.Create(RuntimeLaunchTarget.MainMenu);
        int pulse = ((int)MathF.Floor(elapsedSeconds * 4f) % 3) + 1;
        string dots = new string('.', pulse);

        if (titleText is not null)
        {
            titleText.Text = request.LoadingTitleText;
        }

        if (detailText is not null)
        {
            detailText.Text = request.LoadingDetailText;
        }

        if (statusText is not null)
        {
            statusText.Text = $"{request.LoadingStatusText}{dots}";
        }
    }
}
