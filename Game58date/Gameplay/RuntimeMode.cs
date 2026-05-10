#nullable enable
using System;

namespace Game58date.Gameplay;

public enum RuntimeMode
{
    Terrain = 0,
    Prototype = 1,
    UiShowcase = 2,
}

public static class RuntimeModeResolver
{
    private const string RuntimeModeEnvironmentVariable = "GAME58DATE_RUNTIME_MODE";

    public static RuntimeMode Resolve()
    {
        string? raw = Environment.GetEnvironmentVariable(RuntimeModeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return RuntimeMode.Terrain;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "prototype" => RuntimeMode.Prototype,
            "ui-showcase" => RuntimeMode.UiShowcase,
            "uishowcase" => RuntimeMode.UiShowcase,
            "terrain" => RuntimeMode.Terrain,
            _ => RuntimeMode.Terrain,
        };
    }
}
