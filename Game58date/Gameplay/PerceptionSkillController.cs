#nullable enable
using System;
using Stride.Core.Mathematics;

namespace Game58date.Gameplay;

public sealed class PerceptionSkillController
{
    private const float DefaultDurationSeconds = 6f;
    private const float DefaultCooldownSeconds = 16f;

    public PerceptionSkillController(PerceptionRuntimeState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public PerceptionRuntimeState State { get; }

    public bool TryActivate(float omenScore)
    {
        if (State.IsActive || State.CooldownSecondsRemaining > 0f)
        {
            return false;
        }

        State.IsActive = true;
        State.ActiveSecondsRemaining = DefaultDurationSeconds;
        State.CooldownSecondsRemaining = DefaultCooldownSeconds;
        State.Intensity = MathUtil.Clamp(0.35f + omenScore * 0.65f, 0.35f, 1.0f);
        State.ActivationCount++;
        State.LastActivatedUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public void Tick(float deltaTimeSeconds, float currentOmenScore)
    {
        if (deltaTimeSeconds <= 0f)
        {
            return;
        }

        State.CooldownSecondsRemaining = MathF.Max(0f, State.CooldownSecondsRemaining - deltaTimeSeconds);
        if (!State.IsActive)
        {
            State.Intensity = MathF.Max(0f, State.Intensity - deltaTimeSeconds * 0.4f);
            return;
        }

        State.ActiveSecondsRemaining = MathF.Max(0f, State.ActiveSecondsRemaining - deltaTimeSeconds);
        State.Intensity = MathUtil.Clamp(0.30f + currentOmenScore * 0.70f, 0.30f, 1.0f);

        if (State.ActiveSecondsRemaining > 0f)
        {
            return;
        }

        State.IsActive = false;
        State.ActiveSecondsRemaining = 0f;
    }
}
