#nullable enable
using System;
using Stride.Engine;
using Stride.Input;

namespace Game58date.Gameplay.UI;

public sealed class GameUiRuntimeController : SyncScript, IGameUiCommandSink, IGameUiNoticeSink
{
    private readonly GameUiTheme theme = GameUiTheme.Default;
    private readonly GameUiContextState uiContext = new();
    private readonly GameUiNoticeFeed noticeFeed = new();
    private GameUiComposer? composer;
    private bool lastMenuTogglePressed;
    private bool lastHideUiPressed;
    private bool lastReturnToRouterPressed;

    public WorldLawRuntimeController? WorldLawController { get; private set; }

    public bool ShouldSuspendPlayerControl => uiContext.MenuVisible;

    public override void Start()
    {
        WorldLawController = Entity.Get<WorldLawRuntimeController>();
        if (WorldLawController is null)
        {
            throw new InvalidOperationException("Game UI runtime requires WorldLawRuntimeController on the same entity.");
        }

        composer = new GameUiComposer(theme);
        composer.Attach(Entity, Services, this);
        uiContext.Mode = GameUiMode.Exploration;
        uiContext.DebugHudVisible = WorldLawController.IsDebugHudVisible;
        PushNotice("HUD", "Contextual ritual HUD attached to the world runtime.", GameUiNoticeSeverity.Positive, 4f);
        composer.Update(GameUiStateMapper.Map(WorldLawController.RuntimeState, uiContext, theme));
    }

    public override void Update()
    {
        if (composer is null || WorldLawController is null)
        {
            return;
        }

        HandleUiHotkeys();
        composer.SetVisible(!uiContext.UiHidden);
        WorldLawController.SetDebugHudSuppressed(uiContext.MenuVisible || uiContext.UiHidden);
        WorldLawRuntimeState state = WorldLawController.RuntimeState;
        uiContext.DebugHudVisible = WorldLawController.IsDebugHudVisible;
        uiContext.IntentDraftText = WorldLawController.CurrentIntentDraftText;
        uiContext.Mode = uiContext.MenuVisible
            ? GameUiMode.Atlas
            : (state.Intent.TextInputEnabled ? GameUiMode.InputFocus : GameUiMode.Exploration);
        noticeFeed.Tick((float)Game.UpdateTime.Elapsed.TotalSeconds);
        uiContext.Notices.Clear();
        uiContext.Notices.AddRange(noticeFeed.Snapshot());
        composer.Update(GameUiStateMapper.Map(state, uiContext, theme));
    }

    private void HandleUiHotkeys()
    {
        bool hideUiPressed = Input.IsKeyPressed(Keys.F9);
        if (hideUiPressed && !lastHideUiPressed)
        {
            ToggleUiVisibility();
        }
        lastHideUiPressed = hideUiPressed;

        bool menuPressed = Input.IsKeyPressed(Keys.Escape) || Input.IsKeyPressed(Keys.F10);
        if (menuPressed && !lastMenuTogglePressed && composer is not null)
        {
            ToggleUiMenu();
        }
        lastMenuTogglePressed = menuPressed;

        bool returnToRouterPressed = Input.IsKeyPressed(Keys.R);
        if (uiContext.MenuVisible && returnToRouterPressed && !lastReturnToRouterPressed)
        {
            ReturnToRouter();
        }
        lastReturnToRouterPressed = returnToRouterPressed;
    }

    public void ToggleUiMenu()
    {
        if (uiContext.UiHidden)
        {
            uiContext.UiHidden = false;
        }

        uiContext.MenuVisible = !uiContext.MenuVisible;
        uiContext.Mode = uiContext.MenuVisible ? GameUiMode.Atlas : GameUiMode.Exploration;
        PushNotice("Pause", uiContext.MenuVisible ? "Pause surface opened and mouse released." : "Returned to the exploration surface.", GameUiNoticeSeverity.Info, 3f);
    }

    public void ToggleNarrativeInput()
    {
        if (WorldLawController is null)
        {
            return;
        }

        bool next = !WorldLawController.RuntimeState.Intent.TextInputEnabled;
        WorldLawController.SetIntentTextInputEnabled(next);
        uiContext.Mode = next ? GameUiMode.InputFocus : GameUiMode.Exploration;
        PushNotice("Input", next ? "Narrative input channel engaged." : "Narrative input channel muted.", GameUiNoticeSeverity.Info, 3f);
    }

    public void ToggleDebugHud()
    {
        if (WorldLawController is null)
        {
            return;
        }

        bool next = !WorldLawController.IsDebugHudVisible;
        WorldLawController.SetDebugHudVisible(next);
        uiContext.DebugHudVisible = next;
    }

    public void ToggleUiVisibility()
    {
        uiContext.UiHidden = !uiContext.UiHidden;
        if (uiContext.UiHidden)
        {
            uiContext.MenuVisible = false;
            uiContext.Mode = GameUiMode.Exploration;
            PushNotice("HUD", "All runtime UI layers hidden. Press F9 or Esc to restore them.", GameUiNoticeSeverity.Info, 3f);
            return;
        }

        PushNotice("HUD", "Runtime UI restored.", GameUiNoticeSeverity.Positive, 3f);
    }

    public void ReturnToRouter()
    {
        RuntimeSceneLauncher.Launch(Services, SceneSystem, RuntimeLaunchTarget.DevRouter);
    }

    public void PushNotice(string title, string body, GameUiNoticeSeverity severity, float seconds = 6f)
    {
        noticeFeed.Add(title, body, severity, seconds);
    }
}
