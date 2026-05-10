#nullable enable

namespace Game58date.Gameplay.UI;

public interface IGameUiNoticeSink
{
    void PushNotice(string title, string body, GameUiNoticeSeverity severity, float seconds = 6f);
}
