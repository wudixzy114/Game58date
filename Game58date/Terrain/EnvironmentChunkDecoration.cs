#nullable enable
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class EnvironmentChunkDecoration
{
    public EnvironmentChunkDecoration(Entity? rootEntity, int entityCount)
    {
        RootEntity = rootEntity;
        EntityCount = entityCount;
    }

    public Entity? RootEntity { get; }

    public int EntityCount { get; }
}
