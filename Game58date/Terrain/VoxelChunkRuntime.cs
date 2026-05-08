using Stride.Engine;

namespace Game58date.Terrain;

public sealed class VoxelChunkRuntime
{
    public VoxelChunkRuntime(VoxelChunkCoordinate coordinate, VoxelChunkData data, VoxelChunkMeshData meshData, Entity entity)
    {
        Coordinate = coordinate;
        Data = data;
        MeshData = meshData;
        Entity = entity;
    }

    public VoxelChunkCoordinate Coordinate { get; }

    public VoxelChunkData Data { get; }

    public VoxelChunkMeshData MeshData { get; }

    public Entity Entity { get; }
}
