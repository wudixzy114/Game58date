#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay.UI;

public sealed class GameUiNoticeFeed
{
    private const int MaxNotices = 6;
    private readonly List<GameUiNoticeRecord> notices = new();

    public void Add(string title, string body, GameUiNoticeSeverity severity, float seconds = 6f)
    {
        notices.Add(new GameUiNoticeRecord
        {
            TitleText = title,
            BodyText = body,
            Severity = severity,
            RemainingSeconds = Math.Max(0.25f, seconds),
            AccentColor = GetAccentColor(severity),
            FillColor = GetFillColor(severity),
        });

        while (notices.Count > MaxNotices)
        {
            notices.RemoveAt(0);
        }
    }

    public void Tick(float deltaTimeSeconds)
    {
        if (deltaTimeSeconds <= 0f)
        {
            return;
        }

        for (int index = notices.Count - 1; index >= 0; index--)
        {
            GameUiNoticeRecord notice = notices[index];
            notice.RemainingSeconds = MathF.Max(0f, notice.RemainingSeconds - deltaTimeSeconds);
            if (notice.RemainingSeconds <= 0f)
            {
                notices.RemoveAt(index);
            }
        }
    }

    public GameUiNoticeRecord[] Snapshot()
    {
        int count = Math.Min(3, notices.Count);
        var snapshot = new GameUiNoticeRecord[count];
        for (int index = 0; index < count; index++)
        {
            GameUiNoticeRecord source = notices[notices.Count - 1 - index];
            snapshot[index] = new GameUiNoticeRecord
            {
                TitleText = source.TitleText,
                BodyText = source.BodyText,
                Severity = source.Severity,
                AccentColor = source.AccentColor,
                FillColor = source.FillColor,
                RemainingSeconds = source.RemainingSeconds,
            };
        }

        return snapshot;
    }

    private static Color GetAccentColor(GameUiNoticeSeverity severity)
    {
        return severity switch
        {
            GameUiNoticeSeverity.Positive => new Color(0.52f, 0.87f, 0.63f, 1f),
            GameUiNoticeSeverity.Warning => new Color(0.97f, 0.73f, 0.40f, 1f),
            GameUiNoticeSeverity.Danger => new Color(0.94f, 0.45f, 0.37f, 1f),
            GameUiNoticeSeverity.Omen => new Color(0.40f, 0.82f, 0.93f, 1f),
            _ => new Color(0.93f, 0.78f, 0.46f, 1f),
        };
    }

    private static Color GetFillColor(GameUiNoticeSeverity severity)
    {
        return severity switch
        {
            GameUiNoticeSeverity.Positive => new Color(0.12f, 0.20f, 0.13f, 0.92f),
            GameUiNoticeSeverity.Warning => new Color(0.20f, 0.16f, 0.08f, 0.92f),
            GameUiNoticeSeverity.Danger => new Color(0.22f, 0.08f, 0.07f, 0.92f),
            GameUiNoticeSeverity.Omen => new Color(0.08f, 0.16f, 0.22f, 0.92f),
            _ => new Color(0.10f, 0.11f, 0.13f, 0.92f),
        };
    }
}
