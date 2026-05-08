#nullable enable
using System;

namespace Game58date.Terrain;

public readonly struct VoxelChunkCoordinate : IEquatable<VoxelChunkCoordinate>
{
    public VoxelChunkCoordinate(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }

    public int Z { get; }

    public bool Equals(VoxelChunkCoordinate other)
    {
        return X == other.X && Z == other.Z;
    }

    public override bool Equals(object? obj)
    {
        return obj is VoxelChunkCoordinate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Z);
    }

    public override string ToString()
    {
        return $"{X},{Z}";
    }
}
