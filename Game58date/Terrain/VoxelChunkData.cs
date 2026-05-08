#nullable enable
using System;

namespace Game58date.Terrain;

public sealed class VoxelChunkData
{
    private readonly BlockKind[] blocks;

    public VoxelChunkData(VoxelChunkCoordinate coordinate, int size, int height)
    {
        Coordinate = coordinate;
        Size = size;
        Height = height;
        blocks = new BlockKind[size * height * size];
    }

    public VoxelChunkCoordinate Coordinate { get; }

    public int Size { get; }

    public int Height { get; }

    public BlockKind GetBlock(int x, int y, int z)
    {
        if ((uint)x >= (uint)Size || (uint)z >= (uint)Size || (uint)y >= (uint)Height)
        {
            return BlockKind.Air;
        }

        return blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, BlockKind block)
    {
        if ((uint)x >= (uint)Size || (uint)z >= (uint)Size || (uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException();
        }

        blocks[GetIndex(x, y, z)] = block;
    }

    public int GetSurfaceHeight(int x, int z)
    {
        for (int y = Height - 1; y >= 0; y--)
        {
            if (GetBlock(x, y, z) != BlockKind.Air && GetBlock(x, y, z) != BlockKind.Water)
            {
                return y;
            }
        }

        return 0;
    }

    private int GetIndex(int x, int y, int z)
    {
        return x + Size * (z + Size * y);
    }
}
