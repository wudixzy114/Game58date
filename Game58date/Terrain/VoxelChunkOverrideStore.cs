#nullable enable
using System.Collections.Generic;

namespace Game58date.Terrain;

public sealed class VoxelChunkOverrideStore
{
    private readonly Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>> overridesByChunk = new();

    public void SetOverride(VoxelChunkCoordinate coordinate, int chunkSize, int localX, int localY, int localZ, BlockKind block)
    {
        int index = localX + chunkSize * (localZ + chunkSize * localY);

        if (!overridesByChunk.TryGetValue(coordinate, out Dictionary<int, BlockKind>? chunkOverrides))
        {
            chunkOverrides = new Dictionary<int, BlockKind>();
            overridesByChunk[coordinate] = chunkOverrides;
        }

        chunkOverrides[index] = block;
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
}
