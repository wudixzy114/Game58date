namespace Game58date.Terrain;

public sealed class VoxelChunkMeshData
{
    public VoxelSurfaceMeshData Solid { get; } = new();

    public VoxelSurfaceMeshData Water { get; } = new();

    public int FaceCount => Solid.FaceCount + Water.FaceCount;

    public bool IsEmpty => Solid.IsEmpty && Water.IsEmpty;
}
