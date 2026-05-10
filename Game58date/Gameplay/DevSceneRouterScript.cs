#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Game58date.Gameplay;

public sealed class DevSceneRouterScript : SyncScript
{
    private readonly RuntimeLaunchTarget[] targets =
    {
        RuntimeLaunchTarget.Terrain,
        RuntimeLaunchTarget.Prototype,
        RuntimeLaunchTarget.UiShowcase,
    };

    private UIComponent? uiComponent;
    private TextBlock? selectionText;
    private TextBlock? detailText;
    private TextBlock? hintText;
    private int selectedIndex;
    private bool wasUpPressed;
    private bool wasDownPressed;
    private bool wasWPressed;
    private bool wasSPressed;
    private bool wasEnterPressed;
    private bool wasSpacePressed;
    private bool isLoading;

    public override void Start()
    {
        Game.IsMouseVisible = true;
        EnsureUi();
        UpdateTexts();
    }

    public override void Update()
    {
        HandleNavigation();
    }

    private void HandleNavigation()
    {
        bool upPressed = Input.IsKeyPressed(Keys.Up);
        bool wPressed = Input.IsKeyPressed(Keys.W);
        if ((upPressed && !wasUpPressed) || (wPressed && !wasWPressed))
        {
            selectedIndex = (selectedIndex + targets.Length - 1) % targets.Length;
            UpdateTexts();
        }

        bool downPressed = Input.IsKeyPressed(Keys.Down);
        bool sPressed = Input.IsKeyPressed(Keys.S);
        if ((downPressed && !wasDownPressed) || (sPressed && !wasSPressed))
        {
            selectedIndex = (selectedIndex + 1) % targets.Length;
            UpdateTexts();
        }

        bool enterPressed = Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Return);
        bool spacePressed = Input.IsKeyPressed(Keys.Space);
        if (!isLoading && ((enterPressed && !wasEnterPressed) || (spacePressed && !wasSpacePressed)))
        {
            LaunchSelection();
        }

        wasUpPressed = upPressed;
        wasDownPressed = downPressed;
        wasWPressed = wPressed;
        wasSPressed = sPressed;
        wasEnterPressed = enterPressed;
        wasSpacePressed = spacePressed;
    }

    private void LaunchSelection()
    {
        isLoading = true;
        RuntimeLaunchTarget target = targets[selectedIndex];
        switch (target)
        {
            case RuntimeLaunchTarget.UiShowcase:
                LoadScene("UIShowcaseScene");
                return;
            case RuntimeLaunchTarget.Prototype:
                LoadScene("MainScene");
                PrototypeRuntimeGame.PendingLaunchTarget = RuntimeLaunchTarget.Prototype;
                return;
            default:
                LoadScene("MainScene");
                PrototypeRuntimeGame.PendingLaunchTarget = RuntimeLaunchTarget.Terrain;
                return;
        }
    }

    private void LoadScene(string sceneName)
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        SceneSystem.SceneInstance.RootScene = content.Load<Scene>(sceneName);
        Cancel();
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

        var root = new Canvas
        {
            Width = 1920f,
            Height = 1080f,
        };

        var veil = new Border
        {
            Width = 1920f,
            Height = 1080f,
            BackgroundColor = new Color(0.04f, 0.05f, 0.07f, 0.94f),
            BorderThickness = new Thickness(0f, 0f, 0f, 0f),
        };
        UIElementExtensions.SetCanvasRelativePosition(veil, new Vector3(0f, 0f, 0f));
        root.Children.Add(veil);

        var card = new Border
        {
            Width = 860f,
            Height = 420f,
            BackgroundColor = new Color(0.09f, 0.10f, 0.12f, 0.98f),
            BorderColor = new Color(0.82f, 0.70f, 0.44f, 0.88f),
            BorderThickness = new Thickness(2f, 2f, 2f, 2f),
        };
        UIElementExtensions.SetCanvasRelativePosition(card, new Vector3(0.5f, 0.5f, 0f));
        card.HorizontalAlignment = HorizontalAlignment.Center;
        card.VerticalAlignment = VerticalAlignment.Center;
        root.Children.Add(card);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 760f,
            Height = 332f,
            Margin = new Thickness(40f, 36f, 40f, 36f),
        };
        card.Content = stack;

        stack.Children.Add(new TextBlock
        {
            Text = "GAME58DATE DEV ROUTER",
            TextSize = 34f,
            TextColor = new Color(0.96f, 0.93f, 0.86f, 1f),
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Development startup scene. Choose which runtime slice to enter before the game systems attach.",
            TextSize = 16f,
            TextColor = new Color(0.72f, 0.72f, 0.70f, 1f),
            WrapText = true,
            Height = 48f,
            Margin = new Thickness(0f, 12f, 0f, 0f),
        });

        selectionText = new TextBlock
        {
            TextSize = 24f,
            TextColor = new Color(0.98f, 0.81f, 0.48f, 1f),
            Margin = new Thickness(0f, 28f, 0f, 0f),
        };
        stack.Children.Add(selectionText);

        detailText = new TextBlock
        {
            TextSize = 16f,
            TextColor = new Color(0.86f, 0.88f, 0.91f, 1f),
            WrapText = true,
            Height = 92f,
            Margin = new Thickness(0f, 18f, 0f, 0f),
        };
        stack.Children.Add(detailText);

        hintText = new TextBlock
        {
            TextSize = 15f,
            TextColor = new Color(0.66f, 0.83f, 0.95f, 1f),
            WrapText = true,
            Height = 44f,
            Margin = new Thickness(0f, 22f, 0f, 0f),
        };
        stack.Children.Add(hintText);

        uiComponent.Page = new UIPage { RootElement = root };
    }

    private void UpdateTexts()
    {
        RuntimeLaunchTarget current = targets[selectedIndex];
        if (selectionText is not null)
        {
            selectionText.Text =
                $"[{(selectedIndex == 0 ? ">" : " ")}] Terrain Runtime\n" +
                $"[{(selectedIndex == 1 ? ">" : " ")}] Legacy Prototype\n" +
                $"[{(selectedIndex == 2 ? ">" : " ")}] UI Showcase";
        }

        if (detailText is not null)
        {
            detailText.Text = current switch
            {
                RuntimeLaunchTarget.Terrain => "Loads MainScene and attaches the voxel terrain runtime, world law runtime, save chain, and formal gameplay UI.",
                RuntimeLaunchTarget.Prototype => "Loads MainScene and attaches the older HeroJourney prototype script for quick systemic experiments.",
                RuntimeLaunchTarget.UiShowcase => "Loads the dedicated UIShowcaseScene and runs the curated UI feature presentation scene.",
                _ => string.Empty,
            };
        }

        if (hintText is not null)
        {
            hintText.Text = "Use Up/Down or W/S to choose. Press Enter or Space to launch the selected runtime.";
        }
    }
}
