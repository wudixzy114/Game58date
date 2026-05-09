using Stride.Engine;

namespace Game58date.Terrain;

public sealed class VoxelChunkRuntime
{
    public VoxelChunkRuntime(VoxelChunkCoordinate coordinate, VoxelChunkData data, VoxelChunkMeshData meshData, VoxelChunkCollisionData collisionData, Entity entity)
    {
        Coordinate = coordinate;
        Data = data;
        MeshData = meshData;
        CollisionData = collisionData;
        Entity = entity;
    }

    public VoxelChunkCoordinate Coordinate { get; }

    public VoxelChunkData Data { get; }

    public VoxelChunkMeshData MeshData { get; }

    public VoxelChunkCollisionData CollisionData { get; }

    public Entity Entity { get; }
}
