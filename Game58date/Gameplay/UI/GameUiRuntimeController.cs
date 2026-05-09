#nullable enable
using System;
using Stride.Engine;
using Stride.Input;

namespace Game58date.Gameplay.UI;

public sealed class GameUiRuntimeController : SyncScript
{
    private readonly GameUiTheme theme = GameUiTheme.Default;
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
        composer.Attach(Entity, Game, WorldLawController);
    }

    public override void Update()
    {
        if (composer is null || WorldLawController is null)
        {
            return;
        }

        HandleUiHotkeys();
        WorldLawRuntimeState state = WorldLawController.RuntimeState;
        composer.Update(state);
    }

    private void HandleUiHotkeys()
    {
        bool menuPressed = Input.IsKeyPressed(Keys.Escape) || Input.IsKeyPressed(Keys.F10);
        if (!menuPressed || lastMenuTogglePressed || composer is null)
        {
            lastMenuTogglePressed = menuPressed;
            return;
        }

        composer.ToggleMenu();
        lastMenuTogglePressed = true;
    }
}
