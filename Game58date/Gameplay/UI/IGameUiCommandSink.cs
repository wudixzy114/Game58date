#nullable enable

namespace Game58date.Gameplay.UI;

public interface IGameUiCommandSink
{
    void ToggleUiMenu();

    void ToggleNarrativeInput();

    void ToggleDebugHud();

    void ToggleUiVisibility();

    void ReturnToRouter();
}
