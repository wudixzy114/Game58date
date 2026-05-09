#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Game58date.Save;

public sealed class GameSaveData
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string SlotName { get; set; } = GameSaveRepository.DefaultSlotName;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }

    public WorldSaveData World { get; set; } = new();

    public PlayerSaveData Player { get; set; } = new();

    public TerrainSaveData Terrain { get; set; } = new();

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
