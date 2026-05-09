using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Game58date.Terrain;

public sealed class VoxelSurfaceMeshData
{
    public List<VertexPositionNormalTexture> Vertices { get; } = new();

    public List<int> Indices { get; } = new();

    public int FaceCount => Indices.Count / 6;

    public BoundingBox BoundingBox { get; set; }

    public BoundingSphere BoundingSphere { get; set; }

    public bool IsEmpty => Vertices.Count == 0 || Indices.Count == 0;
}
