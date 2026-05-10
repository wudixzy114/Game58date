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

    public WorldLawRuntimeController? WorldLawController { get; private set; }

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
        bool menuPressed = Input.IsKeyPressed(Keys.Escape) || Input.IsKeyPressed(Keys.F10);
        if (!menuPressed || lastMenuTogglePressed || composer is null)
        {
            lastMenuTogglePressed = menuPressed;
            return;
        }

        ToggleUiMenu();
        lastMenuTogglePressed = true;
    }

    public void ToggleUiMenu()
    {
        uiContext.MenuVisible = !uiContext.MenuVisible;
        uiContext.Mode = uiContext.MenuVisible ? GameUiMode.Atlas : GameUiMode.Exploration;
        PushNotice("Atlas", uiContext.MenuVisible ? "System atlas opened." : "Returned to the exploration surface.", GameUiNoticeSeverity.Info, 3f);
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

    public void PushNotice(string title, string body, GameUiNoticeSeverity severity, float seconds = 6f)
    {
        noticeFeed.Add(title, body, severity, seconds);
    }
}
