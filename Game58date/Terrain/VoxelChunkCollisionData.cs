using System.Collections.Generic;

namespace Game58date.Terrain;

public sealed class VoxelChunkCollisionData
{
    public List<VoxelCollisionBox> Boxes { get; } = new();

    public bool IsEmpty => Boxes.Count == 0;
}
