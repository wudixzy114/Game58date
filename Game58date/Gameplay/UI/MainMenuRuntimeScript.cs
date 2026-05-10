#nullable enable
using System;
using Game58date.Gameplay;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering.Lights;

namespace Game58date.Gameplay.UI;

public sealed class MainMenuRuntimeScript : SyncScript, IMainMenuActionSink
{
    private readonly GameUiTheme theme = GameUiTheme.Default;
    private readonly MainMenuAction[] actions =
    {
        MainMenuAction.Continue,
        MainMenuAction.NewJourney,
        MainMenuAction.UiShowcase,
        MainMenuAction.LegacyPrototype,
        MainMenuAction.Exit,
    };

    private MainMenuComposer? composer;
    private LightComponent? lightComponent;
    private float pulseSeconds;
    private int selectedIndex;
    private bool wasUpPressed;
    private bool wasDownPressed;
    private bool wasEnterPressed;
    private bool wasSpacePressed;
    private bool wasEscapePressed;

    public override void Start()
    {
        pulseSeconds = 0f;
        selectedIndex = 0;
        wasUpPressed = false;
        wasDownPressed = false;
        wasEnterPressed = false;
        wasSpacePressed = false;
        wasEscapePressed = false;
        composer = new MainMenuComposer(theme, Services, this);
        composer.Attach(Entity);

        Entity? lightEntity = FindEntity(Entity.Scene, "Directional light");
        lightComponent = lightEntity?.Get<LightComponent>();
        Game.IsMouseVisible = true;
        composer.Update(BuildViewState());
    }

    public override void Update()
    {
        pulseSeconds += (float)Game.UpdateTime.Elapsed.TotalSeconds;
        HandleInput();
        AnimateLighting();
        composer?.Update(BuildViewState());
    }

    public void ExecuteMainMenuAction(MainMenuAction action)
    {
        switch (action)
        {
            case MainMenuAction.Continue:
                RuntimeSceneLauncher.Launch(Services, SceneSystem, RuntimeLaunchRequest.CreateContinueJourney());
                break;
            case MainMenuAction.NewJourney:
                RuntimeSceneLauncher.Launch(Services, SceneSystem, RuntimeLaunchRequest.CreateNewJourney());
                break;
            case MainMenuAction.UiShowcase:
                RuntimeSceneLauncher.Launch(Services, SceneSystem, RuntimeLaunchTarget.UiShowcase);
                break;
            case MainMenuAction.LegacyPrototype:
                RuntimeSceneLauncher.Launch(Services, SceneSystem, RuntimeLaunchTarget.Prototype);
                break;
            case MainMenuAction.Exit:
                (Game as Stride.Engine.Game)?.Exit();
                break;
        }
    }

    private void HandleInput()
    {
        bool upPressed = Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W);
        bool downPressed = Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S);
        bool enterPressed = Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Return);
        bool spacePressed = Input.IsKeyPressed(Keys.Space);
        bool escapePressed = Input.IsKeyPressed(Keys.Escape);

        if (upPressed && !wasUpPressed)
        {
            selectedIndex = (selectedIndex + actions.Length - 1) % actions.Length;
        }

        if (downPressed && !wasDownPressed)
        {
            selectedIndex = (selectedIndex + 1) % actions.Length;
        }

        if ((enterPressed && !wasEnterPressed) || (spacePressed && !wasSpacePressed))
        {
            ExecuteMainMenuAction(actions[selectedIndex]);
            return;
        }

        if (escapePressed && !wasEscapePressed)
        {
            RuntimeSceneLauncher.Launch(Services, SceneSystem, RuntimeLaunchTarget.DevRouter);
        }

        wasUpPressed = upPressed;
        wasDownPressed = downPressed;
        wasEnterPressed = enterPressed;
        wasSpacePressed = spacePressed;
        wasEscapePressed = escapePressed;
    }

    private MainMenuViewState BuildViewState()
    {
        float pulse = 0.5f + MathF.Sin(pulseSeconds * 0.7f) * 0.5f;
        MainMenuAction selectedAction = actions[selectedIndex];
        Color accent = selectedAction switch
        {
            MainMenuAction.UiShowcase => theme.AccentCyan,
            MainMenuAction.LegacyPrototype => theme.Warning,
            MainMenuAction.Exit => theme.Danger,
            _ => theme.AccentGold,
        };

        return new MainMenuViewState
        {
            AtmosphereAccentColor = accent,
            AtmosphereText = $"Wind Law  {(0.34f + pulse * 0.26f) * 100f:0}%  |  Veil Density  {(0.22f + pulse * 0.18f) * 100f:0}%",
            OmenText = "No quest markers. Only pressure, signs, and landmarks answering what the player dares to ask of the world.",
            JourneyText = $"Current Focus: {GetActionDescriptionTitle(selectedAction)}",
            Options = BuildOptions(),
        };
    }

    private MainMenuOptionViewState[] BuildOptions()
    {
        return new[]
        {
            CreateOption(MainMenuAction.Continue, "Continue Journey", "Enter the formal terrain runtime with contextual exploration UI, omens, and intent flow."),
            CreateOption(MainMenuAction.NewJourney, "Begin New Journey", "Use the same terrain runtime entry as a fresh start point for the current vertical slice."),
            CreateOption(MainMenuAction.UiShowcase, "UI Showcase", "Inspect all current interface states, hierarchy, atmosphere, and systemic overlays in one place."),
            CreateOption(MainMenuAction.LegacyPrototype, "Legacy Prototype", "Compare the old prototype runtime against the new formal UI and systems chain."),
            CreateOption(MainMenuAction.Exit, "Exit", "Close the application from the front-door menu."),
        };
    }

    private MainMenuOptionViewState CreateOption(MainMenuAction action, string label, string description)
    {
        return new MainMenuOptionViewState
        {
            Action = action,
            LabelText = label,
            DescriptionText = description,
            IsSelected = actions[selectedIndex] == action,
        };
    }

    private void AnimateLighting()
    {
        if (lightComponent is null)
        {
            return;
        }

        float pulse = 0.75f + MathF.Sin(pulseSeconds * 0.7f) * 0.25f;
        lightComponent.Intensity = 10f + pulse * 6f;
        lightComponent.SetColor(new Color3(
            0.88f + pulse * 0.12f,
            0.84f + pulse * 0.08f,
            0.76f + pulse * 0.06f));
    }

    private static Entity? FindEntity(Scene? scene, string name)
    {
        if (scene is null)
        {
            return null;
        }

        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name == name)
            {
                return entity;
            }
        }

        return null;
    }

    private static string GetActionDescriptionTitle(MainMenuAction action)
    {
        return action switch
        {
            MainMenuAction.Continue => "Continue Journey",
            MainMenuAction.NewJourney => "Begin New Journey",
            MainMenuAction.UiShowcase => "UI Showcase",
            MainMenuAction.LegacyPrototype => "Legacy Prototype",
            MainMenuAction.Exit => "Exit",
            _ => "Unknown",
        };
    }
}
