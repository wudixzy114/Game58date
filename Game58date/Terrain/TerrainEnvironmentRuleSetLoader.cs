#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Game58date.Terrain;

public static class TerrainEnvironmentRuleSetLoader
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static TerrainEnvironmentRuleSet LoadOrCreateDefault(string? rootDirectory)
    {
        string configPath = ResolveConfigPath(rootDirectory);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            if (!File.Exists(configPath))
            {
                TerrainEnvironmentRuleSet defaults = TerrainEnvironmentRuleSet.CreateDefault();
                Save(configPath, defaults);
                return defaults;
            }

            string json = File.ReadAllText(configPath, Utf8NoBom);
            EnvironmentRuleSetConfig? config = JsonSerializer.Deserialize<EnvironmentRuleSetConfig>(json, JsonOptions);
            TerrainEnvironmentRuleSet loaded = config is null
                ? TerrainEnvironmentRuleSet.CreateDefault()
                : TerrainEnvironmentRuleSet.FromConfig(config);

            if (!loaded.HasAnyRules)
            {
                loaded = TerrainEnvironmentRuleSet.CreateDefault();
            }

            return loaded;
        }
        catch (Exception exception)
        {
            TerrainRuntimeLogger.Logger.Warning($"Failed to load environment rule config '{configPath}'. Falling back to defaults. {exception.Message}");
            return TerrainEnvironmentRuleSet.CreateDefault();
        }
    }

    public static void Save(string configPath, TerrainEnvironmentRuleSet ruleSet)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        string json = JsonSerializer.Serialize(ruleSet.ToConfig(), JsonOptions);
        File.WriteAllText(configPath, json, Utf8NoBom);
    }

    private static string ResolveConfigPath(string? rootDirectory)
    {
        string baseDirectory = string.IsNullOrWhiteSpace(rootDirectory)
            ? AppContext.BaseDirectory
            : rootDirectory;
        return Path.Combine(baseDirectory, "Config", "terrain-environment-rules.json");
    }
}
