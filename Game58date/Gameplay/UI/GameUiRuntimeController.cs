#nullable enable
using System;
using Stride.Engine;
using Stride.Input;

namespace Game58date.Gameplay.UI;

public sealed class GameUiRuntimeController : SyncScript, IGameUiCommandSink
{
    private readonly GameUiTheme theme = GameUiTheme.Default;
    private readonly GameUiContextState uiContext = new();
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
        uiContext.DebugHudVisible = WorldLawController.IsDebugHudVisible;
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
    }

    public void ToggleNarrativeInput()
    {
        if (WorldLawController is null)
        {
            return;
        }

        bool next = !WorldLawController.RuntimeState.Intent.TextInputEnabled;
        WorldLawController.SetIntentTextInputEnabled(next);
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
}
