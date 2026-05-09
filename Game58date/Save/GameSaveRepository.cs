#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Stride.Core.Diagnostics;

namespace Game58date.Save;

public sealed class GameSaveRepository
{
    public const string DefaultSlotName = "main";

    private const string SaveDirectoryName = "Saves";
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public GameSaveRepository(string? rootDirectory = null)
    {
        RootDirectory = string.IsNullOrWhiteSpace(rootDirectory)
            ? BuildDefaultRootDirectory()
            : rootDirectory;
    }

    public string RootDirectory { get; }

    public GameSaveData LoadOrCreate(string slotName, int preferredSeed)
    {
        string sanitizedSlotName = SanitizeSlotName(slotName);
        string slotPath = GetSlotPath(sanitizedSlotName);

        try
        {
            Directory.CreateDirectory(RootDirectory);
            if (!File.Exists(slotPath))
            {
                GameSaveData created = GameSaveData.CreateNew(sanitizedSlotName, preferredSeed);
                Save(created);
                GameSaveLogger.Logger.Info($"Created new save slot '{sanitizedSlotName}' at '{slotPath}' with seed {preferredSeed}.");
                return created;
            }

            string json = File.ReadAllText(slotPath, Utf8NoBom);
            GameSaveData? loaded = JsonSerializer.Deserialize<GameSaveData>(json, JsonOptions);
            if (loaded is null)
            {
                throw new InvalidOperationException($"Save slot '{sanitizedSlotName}' deserialized to null.");
            }

            Normalize(loaded, sanitizedSlotName, preferredSeed);
            GameSaveLogger.Logger.Info($"Loaded save slot '{sanitizedSlotName}' from '{slotPath}'.");
            return loaded;
        }
        catch (Exception exception)
        {
            BackupCorruptSave(slotPath);
            GameSaveLogger.Logger.Error($"Failed to load save slot '{sanitizedSlotName}' from '{slotPath}'. Falling back to a fresh save. {exception}");

            GameSaveData recovered = GameSaveData.CreateNew(sanitizedSlotName, preferredSeed);
            Save(recovered);
            return recovered;
        }
    }

    public bool Save(GameSaveData saveData)
    {
        string sanitizedSlotName = SanitizeSlotName(saveData.SlotName);
        string slotPath = GetSlotPath(sanitizedSlotName);
        string tempPath = $"{slotPath}.tmp";

        try
        {
            Directory.CreateDirectory(RootDirectory);
            Normalize(saveData, sanitizedSlotName, fallbackSeed: 0);

            saveData.SchemaVersion = GameSaveData.CurrentSchemaVersion;
            saveData.SlotName = sanitizedSlotName;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (saveData.CreatedUtc == default)
            {
                saveData.CreatedUtc = now;
            }

            saveData.UpdatedUtc = now;

            string json = JsonSerializer.Serialize(saveData, JsonOptions);
            File.WriteAllText(tempPath, json, Utf8NoBom);
            File.Move(tempPath, slotPath, overwrite: true);
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Ignore cleanup failures after the primary save error has already been reported.
            }

            GameSaveLogger.Logger.Error($"Failed to save slot '{sanitizedSlotName}' to '{slotPath}'. {exception}");
            return false;
        }
    }

    public string GetSlotPath(string slotName)
    {
        return Path.Combine(RootDirectory, $"{SanitizeSlotName(slotName)}.json");
    }

    private static void Normalize(GameSaveData saveData, string sanitizedSlotName, int fallbackSeed)
    {
        saveData.SchemaVersion = saveData.SchemaVersion <= 0
            ? GameSaveData.CurrentSchemaVersion
            : saveData.SchemaVersion;
        saveData.SlotName = sanitizedSlotName;

        saveData.World ??= new WorldSaveData();
        saveData.World.Seed ??= fallbackSeed;

        saveData.Player ??= new PlayerSaveData();
        if (saveData.Player.EyePosition is not null && !saveData.Player.EyePosition.IsFinite)
        {
            saveData.Player.EyePosition = null;
        }

        if (saveData.Player.Rotation is not null && !saveData.Player.Rotation.IsFiniteAndNonZero)
        {
            saveData.Player.Rotation = null;
        }

        saveData.Terrain ??= new TerrainSaveData();
        saveData.Terrain.ChunkOverrides ??= new System.Collections.Generic.List<ChunkOverrideSaveData>();

        foreach (ChunkOverrideSaveData chunkOverride in saveData.Terrain.ChunkOverrides)
        {
            chunkOverride.Blocks ??= new System.Collections.Generic.List<BlockOverrideSaveData>();
        }
    }

    private static void BackupCorruptSave(string slotPath)
    {
        if (!File.Exists(slotPath))
        {
            return;
        }

        try
        {
            string corruptPath = $"{slotPath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
            File.Move(slotPath, corruptPath, overwrite: true);
            GameSaveLogger.Logger.Warning($"Backed up unreadable save file to '{corruptPath}'.");
        }
        catch (Exception exception)
        {
            GameSaveLogger.Logger.Error($"Failed to back up unreadable save file '{slotPath}'. {exception}");
        }
    }

    private static string BuildDefaultRootDirectory()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = AppContext.BaseDirectory;
        }

        return Path.Combine(localAppData, "Game58date", SaveDirectoryName);
    }

    private static string SanitizeSlotName(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName))
        {
            return DefaultSlotName;
        }

        char[] buffer = new char[slotName.Length];
        int bufferIndex = 0;

        foreach (char character in slotName)
        {
            buffer[bufferIndex++] = char.IsLetterOrDigit(character) || character is '-' or '_'
                ? character
                : '_';
        }

        string sanitized = new string(buffer, 0, bufferIndex).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized)
            ? DefaultSlotName
            : sanitized;
    }
}

public static class GameSaveLogger
{
    public static readonly Logger Logger = GlobalLogger.GetLogger("GameSave");
}
