#nullable enable
using System;
using System.Collections.Generic;
using Game58date.Gameplay;
using Stride.Core.Mathematics;

namespace Game58date.Save;

public sealed class GameSaveData
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string SlotName { get; set; } = GameSaveRepository.DefaultSlotName;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }

    public WorldSaveData World { get; set; } = new();

    public PlayerSaveData Player { get; set; } = new();

    public TerrainSaveData Terrain { get; set; } = new();

    public GameplaySaveData Gameplay { get; set; } = new();

    public static GameSaveData CreateNew(string slotName, int seed)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new GameSaveData
        {
            SlotName = slotName,
            CreatedUtc = now,
            UpdatedUtc = now,
            World = new WorldSaveData
            {
                Seed = seed,
            },
            Player = new PlayerSaveData(),
            Terrain = new TerrainSaveData(),
            Gameplay = new GameplaySaveData(),
        };
    }
}

public sealed class WorldSaveData
{
    public int? Seed { get; set; }
}

public sealed class PlayerSaveData
{
    public SerializableVector3? EyePosition { get; set; }

    public SerializableQuaternion? Rotation { get; set; }
}

public sealed class TerrainSaveData
{
    public int ChunkSize { get; set; }

    public int ChunkHeight { get; set; }

    public List<ChunkOverrideSaveData> ChunkOverrides { get; set; } = new();
}

public sealed class GameplaySaveData
{
    public WorldLawSaveData WorldLaw { get; set; } = new();

    public PlayerIntentSaveData Intent { get; set; } = new();

    public HeroJourneySaveData Narrative { get; set; } = new();

    public OmenSaveData Omen { get; set; } = new();
}

public sealed class ChunkOverrideSaveData
{
    public int ChunkX { get; set; }

    public int ChunkZ { get; set; }

    public List<BlockOverrideSaveData> Blocks { get; set; } = new();
}

public sealed class BlockOverrideSaveData
{
    public int Index { get; set; }

    public byte Block { get; set; }
}

public sealed class WorldLawSaveData
{
    public HeroJourneyStage HeroStage { get; set; }

    public float OmenCooldownSeconds { get; set; }

    public float OmenVisualSeconds { get; set; }

    public float WorldTimeSeconds { get; set; }

    public int RuntimeRandomSeed { get; set; } = 5800;

    public WorldLawStateSaveData World { get; set; } = new();

    public PlayerBehaviorSaveData Behavior { get; set; } = new();

    public List<CausalityRecordSaveData> RecentCausality { get; set; } = new();
}

public sealed class WorldLawStateSaveData
{
    public float ExplorationDrive { get; set; } = 0.08f;

    public float BorderLonging { get; set; } = 0.05f;

    public float BlessingWeight { get; set; } = 0.10f;

    public float ResourcePressure { get; set; } = 0.10f;

    public float Karma { get; set; }

    public float Violence { get; set; } = 0.10f;

    public float Faith { get; set; } = 0.08f;

    public float Curiosity { get; set; } = 0.12f;

    public float PathVisibility { get; set; }

    public float SocialFlux { get; set; } = 0.05f;

    public float Atmosphere { get; set; } = 0.10f;

    public float LossMemory { get; set; }

    public int IntentCount { get; set; }

    public int OmenCount { get; set; }

    public OmenType LastOmen { get; set; }

    public string LastOmenDescription { get; set; } = "No omen yet.";

    public string TargetBiome { get; set; } = "Morning Plain";
}

public sealed class PlayerBehaviorSaveData
{
    public float PeacefulTendency { get; set; } = 0.10f;

    public float ViolentTendency { get; set; } = 0.10f;

    public float ExplorationTendency { get; set; } = 0.10f;

    public float FaithTendency { get; set; } = 0.08f;

    public float CuriosityTendency { get; set; } = 0.12f;

    public int RecordedActions { get; set; }

    public int IntentActions { get; set; }

    public int PeacefulActions { get; set; }

    public int ViolentActions { get; set; }

    public int LossEvents { get; set; }
}

public sealed class CausalityRecordSaveData
{
    public DateTimeOffset TimestampUtc { get; set; }

    public PlayerActionKind ActionKind { get; set; }

    public string Summary { get; set; } = string.Empty;

    public float KarmaDelta { get; set; }

    public float BlessingDelta { get; set; }

    public OmenType TriggeredOmen { get; set; }
}

public sealed class PlayerIntentSaveData
{
    public bool TextInputEnabled { get; set; } = true;

    public int SubmittedIntentCount { get; set; }

    public PlayerIntentRecordSaveData? LastIntent { get; set; }

    public List<PlayerIntentRecordSaveData> RecentIntents { get; set; } = new();
}

public sealed class PlayerIntentRecordSaveData
{
    public DateTimeOffset TimestampUtc { get; set; }

    public string RawText { get; set; } = string.Empty;

    public IntentTopic Topic { get; set; }

    public float Confidence { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string SuggestedTargetBiome { get; set; } = string.Empty;
}

public sealed class HeroJourneySaveData
{
    public HeroJourneyStage CurrentStage { get; set; }

    public string LastStageReason { get; set; } = "Initial state.";

    public DateTimeOffset? LastAdvancedUtc { get; set; }

    public List<HeroJourneyStageRecordSaveData> StageHistory { get; set; } = new();
}

public sealed class HeroJourneyStageRecordSaveData
{
    public HeroJourneyStage Stage { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public sealed class OmenSaveData
{
    public float CooldownSeconds { get; set; }

    public float VisualSeconds { get; set; }

    public float LastScore { get; set; }

    public OmenSource LastSource { get; set; }

    public OmenRecordSaveData? ActiveOmen { get; set; }

    public List<OmenRecordSaveData> History { get; set; } = new();
}

public sealed class OmenRecordSaveData
{
    public DateTimeOffset TimestampUtc { get; set; }

    public OmenType OmenType { get; set; }

    public OmenSource Source { get; set; }

    public float Score { get; set; }

    public string Description { get; set; } = string.Empty;
}

public sealed class SerializableVector3
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);

    public Vector3 ToStrideVector3()
    {
        return new Vector3(X, Y, Z);
    }

    public static SerializableVector3 FromStride(Vector3 value)
    {
        return new SerializableVector3
        {
            X = value.X,
            Y = value.Y,
            Z = value.Z,
        };
    }
}

public sealed class SerializableQuaternion
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public float W { get; set; }

    public bool IsFiniteAndNonZero
    {
        get
        {
            if (!float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(Z) || !float.IsFinite(W))
            {
                return false;
            }

            float lengthSquared = (X * X) + (Y * Y) + (Z * Z) + (W * W);
            return lengthSquared > 0.0001f;
        }
    }

    public Quaternion ToStrideQuaternion()
    {
        return new Quaternion(X, Y, Z, W);
    }

    public static SerializableQuaternion FromStride(Quaternion value)
    {
        return new SerializableQuaternion
        {
            X = value.X,
            Y = value.Y,
            Z = value.Z,
            W = value.W,
        };
    }
}
