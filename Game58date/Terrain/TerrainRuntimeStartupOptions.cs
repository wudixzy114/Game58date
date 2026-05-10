#nullable enable
using System;
using System.Globalization;
using Game58date.Save;

namespace Game58date.Terrain;

public sealed class TerrainRuntimeStartupOptions
{
    private const string SaveSlotEnvironmentVariable = "GAME58DATE_SAVE_SLOT";
    private const string WorldSeedEnvironmentVariable = "GAME58DATE_WORLD_SEED";
    private const string AutosaveEnvironmentVariable = "GAME58DATE_AUTOSAVE_SECONDS";
    private const float DefaultAutosaveIntervalSeconds = 12f;
    private static TerrainRuntimeLaunchOverrides? pendingLaunchOverrides;

    public string SaveSlotName { get; init; } = GameSaveRepository.DefaultSlotName;

    public int PreferredSeed { get; init; } = TerrainGenerationSettings.DefaultSeed;

    public float AutosaveIntervalSeconds { get; init; } = DefaultAutosaveIntervalSeconds;

    public bool StartFreshJourney { get; init; }

    public bool UseSavedPlayerPose { get; init; } = true;

    public bool AutosaveEnabled => AutosaveIntervalSeconds > 0f;

    public static void SetPendingLaunchOverrides(TerrainRuntimeLaunchOverrides? launchOverrides)
    {
        pendingLaunchOverrides = launchOverrides;
    }

    public static TerrainRuntimeStartupOptions FromEnvironment(TerrainGenerationSettings defaults)
    {
        string saveSlotName = ReadTrimmedEnvironmentVariable(SaveSlotEnvironmentVariable) ?? GameSaveRepository.DefaultSlotName;
        int preferredSeed = ReadIntEnvironmentVariable(WorldSeedEnvironmentVariable) ?? defaults.Seed;
        float autosaveIntervalSeconds = ReadFloatEnvironmentVariable(AutosaveEnvironmentVariable) ?? DefaultAutosaveIntervalSeconds;
        TerrainRuntimeLaunchOverrides? launchOverrides = ConsumePendingLaunchOverrides();

        if (!string.IsNullOrWhiteSpace(launchOverrides?.SaveSlotName))
        {
            saveSlotName = launchOverrides.SaveSlotName!.Trim();
        }

        if (launchOverrides?.PreferredSeed is int overriddenSeed)
        {
            preferredSeed = overriddenSeed;
        }

        if (launchOverrides?.AutosaveIntervalSeconds is float overriddenAutosave)
        {
            autosaveIntervalSeconds = overriddenAutosave;
        }

        if (autosaveIntervalSeconds < 0f)
        {
            TerrainRuntimeLogger.Logger.Warning($"{AutosaveEnvironmentVariable} cannot be negative. Autosave has been disabled.");
            autosaveIntervalSeconds = 0f;
        }

        bool startFreshJourney = launchOverrides?.StartFreshJourney ?? false;
        bool useSavedPlayerPose = launchOverrides?.UseSavedPlayerPose ?? !startFreshJourney;

        TerrainRuntimeLogger.Logger.Info(
            $"Startup options resolved slot='{saveSlotName}' preferredSeed={preferredSeed} autosave={autosaveIntervalSeconds:0.##}s fresh={startFreshJourney} useSavedPose={useSavedPlayerPose}.");

        return new TerrainRuntimeStartupOptions
        {
            SaveSlotName = saveSlotName,
            PreferredSeed = preferredSeed,
            AutosaveIntervalSeconds = autosaveIntervalSeconds,
            StartFreshJourney = startFreshJourney,
            UseSavedPlayerPose = useSavedPlayerPose,
        };
    }

    private static TerrainRuntimeLaunchOverrides? ConsumePendingLaunchOverrides()
    {
        TerrainRuntimeLaunchOverrides? launchOverrides = pendingLaunchOverrides;
        pendingLaunchOverrides = null;
        return launchOverrides;
    }

    private static string? ReadTrimmedEnvironmentVariable(string variableName)
    {
        string? value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static int? ReadIntEnvironmentVariable(string variableName)
    {
        string? value = ReadTrimmedEnvironmentVariable(variableName);
        if (value is null)
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        TerrainRuntimeLogger.Logger.Warning($"{variableName}='{value}' is not a valid integer and has been ignored.");
        return null;
    }

    private static float? ReadFloatEnvironmentVariable(string variableName)
    {
        string? value = ReadTrimmedEnvironmentVariable(variableName);
        if (value is null)
        {
            return null;
        }

        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsed))
        {
            return parsed;
        }

        TerrainRuntimeLogger.Logger.Warning($"{variableName}='{value}' is not a valid float and has been ignored.");
        return null;
    }
}
