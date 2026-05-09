#nullable enable
using System.Collections.Generic;

namespace Game58date.Terrain;

public sealed class VoxelChunkOverrideStore
{
    private readonly Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>> overridesByChunk = new();

    public int Revision { get; private set; }

    public void SetOverride(VoxelChunkCoordinate coordinate, int chunkSize, int localX, int localY, int localZ, BlockKind block)
    {
        int index = localX + chunkSize * (localZ + chunkSize * localY);

        if (!overridesByChunk.TryGetValue(coordinate, out Dictionary<int, BlockKind>? chunkOverrides))
        {
            chunkOverrides = new Dictionary<int, BlockKind>();
            overridesByChunk[coordinate] = chunkOverrides;
        }

        if (chunkOverrides.TryGetValue(index, out BlockKind existing) && existing == block)
        {
            return;
        }

        chunkOverrides[index] = block;
        Revision++;
    }

    public bool TryGetChunkOverrides(VoxelChunkCoordinate coordinate, out IReadOnlyDictionary<int, BlockKind>? chunkOverrides)
    {
        if (overridesByChunk.TryGetValue(coordinate, out Dictionary<int, BlockKind>? found))
        {
            chunkOverrides = found;
            return true;
        }

        chunkOverrides = null;
        return false;
    }

    public Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>> CloneAll()
    {
        var clone = new Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>>(overridesByChunk.Count);
        foreach ((VoxelChunkCoordinate coordinate, Dictionary<int, BlockKind> chunkOverrides) in overridesByChunk)
        {
            clone[coordinate] = new Dictionary<int, BlockKind>(chunkOverrides);
        }

        return clone;
    }

    public void ReplaceAll(Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>> snapshot)
    {
        bool hadEntries = overridesByChunk.Count > 0;
        overridesByChunk.Clear();

        foreach ((VoxelChunkCoordinate coordinate, Dictionary<int, BlockKind> chunkOverrides) in snapshot)
        {
            if (chunkOverrides.Count == 0)
            {
                continue;
            }

            overridesByChunk[coordinate] = new Dictionary<int, BlockKind>(chunkOverrides);
        }

        if (hadEntries || overridesByChunk.Count > 0)
        {
            Revision++;
        }
    }

    public int GetTotalOverrideCount()
    {
        int total = 0;
        foreach (Dictionary<int, BlockKind> chunkOverrides in overridesByChunk.Values)
        {
            total += chunkOverrides.Count;
        }

        return total;
    }
}
