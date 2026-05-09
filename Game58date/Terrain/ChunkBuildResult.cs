namespace Game58date.Terrain;

public sealed class ChunkBuildResult
{
    public ChunkBuildResult(int revision, VoxelChunkCoordinate coordinate, VoxelChunkData data, VoxelChunkMeshData meshData, VoxelChunkCollisionData collisionData)
    {
        Revision = revision;
        Coordinate = coordinate;
        Data = data;
        MeshData = meshData;
        CollisionData = collisionData;
    }

    public int Revision { get; }

    public VoxelChunkCoordinate Coordinate { get; }

    public VoxelChunkData Data { get; }

    public VoxelChunkMeshData MeshData { get; }

    public VoxelChunkCollisionData CollisionData { get; }
}
