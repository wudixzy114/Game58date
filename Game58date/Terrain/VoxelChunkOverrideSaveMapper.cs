#nullable enable
using System.Collections.Generic;
using System.Linq;
using Game58date.Save;

namespace Game58date.Terrain;

public static class VoxelChunkOverrideSaveMapper
{
    public static TerrainSaveData CreateTerrainSaveData(TerrainGenerationSettings settings, VoxelChunkOverrideStore overrideStore)
    {
        var terrainSaveData = new TerrainSaveData
        {
            ChunkSize = settings.ChunkSize,
            ChunkHeight = settings.ChunkHeight,
        };

        Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>> snapshot = overrideStore.CloneAll();
        foreach ((VoxelChunkCoordinate coordinate, Dictionary<int, BlockKind> chunkOverrides) in snapshot.OrderBy(entry => entry.Key.X).ThenBy(entry => entry.Key.Z))
        {
            if (chunkOverrides.Count == 0)
            {
                continue;
            }

            var chunkSaveData = new ChunkOverrideSaveData
            {
                ChunkX = coordinate.X,
                ChunkZ = coordinate.Z,
            };

            foreach ((int index, BlockKind block) in chunkOverrides.OrderBy(entry => entry.Key))
            {
                chunkSaveData.Blocks.Add(new BlockOverrideSaveData
                {
                    Index = index,
                    Block = (byte)block,
                });
            }

            terrainSaveData.ChunkOverrides.Add(chunkSaveData);
        }

        return terrainSaveData;
    }

    public static Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>> BuildOverrideSnapshot(TerrainSaveData? terrainSaveData, TerrainGenerationSettings settings)
    {
        var snapshot = new Dictionary<VoxelChunkCoordinate, Dictionary<int, BlockKind>>();
        if (terrainSaveData is null)
        {
            return snapshot;
        }

        if (terrainSaveData.ChunkOverrides.Count == 0)
        {
            return snapshot;
        }

        if (terrainSaveData.ChunkSize != settings.ChunkSize || terrainSaveData.ChunkHeight != settings.ChunkHeight)
        {
            TerrainRuntimeLogger.Logger.Warning(
                $"Ignored terrain overrides from save because dimensions changed. saved=({terrainSaveData.ChunkSize},{terrainSaveData.ChunkHeight}) current=({settings.ChunkSize},{settings.ChunkHeight}).");
            return snapshot;
        }

        int maxIndexExclusive = settings.ChunkSize * settings.ChunkSize * settings.ChunkHeight;
        foreach (ChunkOverrideSaveData chunkSaveData in terrainSaveData.ChunkOverrides)
        {
            if (chunkSaveData.Blocks.Count == 0)
            {
                continue;
            }

            var coordinate = new VoxelChunkCoordinate(chunkSaveData.ChunkX, chunkSaveData.ChunkZ);
            var chunkOverrides = new Dictionary<int, BlockKind>(chunkSaveData.Blocks.Count);

            foreach (BlockOverrideSaveData blockSaveData in chunkSaveData.Blocks)
            {
                if (blockSaveData.Index < 0 || blockSaveData.Index >= maxIndexExclusive)
                {
                    TerrainRuntimeLogger.Logger.Warning($"Ignored invalid block override index {blockSaveData.Index} in chunk {coordinate}.");
                    continue;
                }

                if (!TryConvertBlock(blockSaveData.Block, out BlockKind block))
                {
                    TerrainRuntimeLogger.Logger.Warning($"Ignored invalid block kind value {blockSaveData.Block} in chunk {coordinate} index {blockSaveData.Index}.");
                    continue;
                }

                chunkOverrides[blockSaveData.Index] = block;
            }

            if (chunkOverrides.Count > 0)
            {
                snapshot[coordinate] = chunkOverrides;
            }
        }

        return snapshot;
    }

    private static bool TryConvertBlock(byte rawBlock, out BlockKind block)
    {
        block = (BlockKind)rawBlock;
        return block is BlockKind.Air
            or BlockKind.Bedrock
            or BlockKind.Stone
            or BlockKind.Dirt
            or BlockKind.Grass
            or BlockKind.Sand
            or BlockKind.Water
            or BlockKind.Mud
            or BlockKind.Peat
            or BlockKind.Moss
            or BlockKind.Snow
            or BlockKind.Scree;
    }
}
