#nullable enable
using System;
using System.Linq;
using Game58date.Gameplay.UI;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Game58date.Terrain;

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
    private SpriteFont? font;
    private TextBlock? selectionText;
    private TextBlock? detailText;
    private TextBlock? hintText;
    private Border? optionTerrain;
    private Border? optionPrototype;
    private Border? optionShowcase;
    private TextBlock? optionTerrainText;
    private TextBlock? optionPrototypeText;
    private TextBlock? optionShowcaseText;
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
        font = GameUiFontProvider.Load(Services);
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
                LoadMainScene(RuntimeLaunchTarget.Prototype);
                return;
            default:
                LoadMainScene(RuntimeLaunchTarget.Terrain);
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

    private void LoadMainScene(RuntimeLaunchTarget target)
    {
        IContentManager? content = Services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        Scene? scene = content.Load<Scene>("MainScene");
        if (scene is null)
        {
            throw new InvalidOperationException("The asset 'MainScene' could not be loaded. Ensure it is registered in the package RootAssets.");
        }

        SceneSystem.SceneInstance.RootScene = scene;

        string anchorName = target == RuntimeLaunchTarget.Prototype
            ? "LegacyPrototypeRuntime"
            : "PrototypeRuntime";

        Entity? anchor = scene.Entities.FirstOrDefault(entity => entity.Name == anchorName);
        if (anchor is null)
        {
            anchor = new Entity(anchorName);
            if (target == RuntimeLaunchTarget.Prototype)
            {
                anchor.Add(new HeroJourneyPrototypeScript());
            }
            else
            {
                anchor.Add(new VoxelTerrainRuntimeScript());
            }

            scene.Entities.Add(anchor);
        }

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
        UIElementExtensions.SetCanvasPinOrigin(veil, new Vector3(0f, 0f, 0f));
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
        UIElementExtensions.SetCanvasPinOrigin(card, new Vector3(0.5f, 0.5f, 0f));
        UIElementExtensions.SetCanvasRelativePosition(card, new Vector3(0.5f, 0.5f, 0f));
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
            Font = font,
            TextSize = 34f,
            TextColor = new Color(0.96f, 0.93f, 0.86f, 1f),
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Development startup scene. Choose which runtime slice to enter before the game systems attach.",
            Font = font,
            TextSize = 16f,
            TextColor = new Color(0.72f, 0.72f, 0.70f, 1f),
            WrapText = true,
            Height = 48f,
            Margin = new Thickness(0f, 12f, 0f, 0f),
        });

        var optionsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Width = 760f,
            Height = 72f,
            Margin = new Thickness(0f, 28f, 0f, 0f),
        };
        stack.Children.Add(optionsRow);

        (optionTerrain, optionTerrainText) = CreateOptionCard("Terrain Runtime");
        (optionPrototype, optionPrototypeText) = CreateOptionCard("Legacy Prototype");
        (optionShowcase, optionShowcaseText) = CreateOptionCard("UI Showcase");
        optionsRow.Children.Add(optionTerrain);
        optionsRow.Children.Add(optionPrototype);
        optionsRow.Children.Add(optionShowcase);

        selectionText = new TextBlock
        {
            Font = font,
            TextSize = 18f,
            TextColor = new Color(0.98f, 0.81f, 0.48f, 1f),
            Margin = new Thickness(0f, 16f, 0f, 0f),
        };
        stack.Children.Add(selectionText);

        detailText = new TextBlock
        {
            Font = font,
            TextSize = 16f,
            TextColor = new Color(0.86f, 0.88f, 0.91f, 1f),
            WrapText = true,
            Height = 92f,
            Margin = new Thickness(0f, 18f, 0f, 0f),
        };
        stack.Children.Add(detailText);

        hintText = new TextBlock
        {
            Font = font,
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
            selectionText.Text = $"Selected: {GetTargetTitle(current)}";
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

        UpdateOptionCard(optionTerrain, optionTerrainText, 0, "Terrain Runtime");
        UpdateOptionCard(optionPrototype, optionPrototypeText, 1, "Legacy Prototype");
        UpdateOptionCard(optionShowcase, optionShowcaseText, 2, "UI Showcase");
    }

    private (Border Border, TextBlock Text) CreateOptionCard(string title)
    {
        var text = new TextBlock
        {
            Font = font,
            Text = title,
            TextSize = 18f,
            TextColor = new Color(0.96f, 0.93f, 0.86f, 1f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var border = new Border
        {
            Width = 236f,
            Height = 68f,
            Margin = new Thickness(0f, 0f, 16f, 0f),
            BackgroundColor = new Color(0.16f, 0.17f, 0.20f, 1f),
            BorderColor = new Color(0.34f, 0.36f, 0.40f, 1f),
            BorderThickness = new Thickness(2f, 2f, 2f, 2f),
            Content = text,
        };

        return (border, text);
    }

    private void UpdateOptionCard(Border? border, TextBlock? text, int index, string title)
    {
        if (border is null || text is null)
        {
            return;
        }

        bool selected = selectedIndex == index;
        border.BackgroundColor = selected
            ? new Color(0.28f, 0.22f, 0.12f, 1f)
            : new Color(0.16f, 0.17f, 0.20f, 1f);
        border.BorderColor = selected
            ? new Color(0.98f, 0.81f, 0.48f, 1f)
            : new Color(0.34f, 0.36f, 0.40f, 1f);
        text.TextColor = selected
            ? new Color(1.0f, 0.95f, 0.82f, 1f)
            : new Color(0.82f, 0.84f, 0.87f, 1f);
        text.Text = selected ? $"> {title}" : title;
    }

    private static string GetTargetTitle(RuntimeLaunchTarget target)
    {
        return target switch
        {
            RuntimeLaunchTarget.Terrain => "Terrain Runtime",
            RuntimeLaunchTarget.Prototype => "Legacy Prototype",
            RuntimeLaunchTarget.UiShowcase => "UI Showcase",
            _ => "Unknown",
        };
    }
}
