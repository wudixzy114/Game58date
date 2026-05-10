#nullable enable
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class EnvironmentEntityPool
{
    private readonly Dictionary<string, Stack<Entity>> pooledEntities = new();
    private int pooledCount;

    public int PooledCount => pooledCount;

    public Entity GetOrCreate(string poolKey, System.Func<Entity> factory)
    {
        if (pooledEntities.TryGetValue(poolKey, out Stack<Entity>? entities) && entities.Count > 0)
        {
            pooledCount--;
            return entities.Pop();
        }

        return factory();
    }

    public void Return(string poolKey, Entity entity)
    {
        ResetEntity(entity);
        if (!pooledEntities.TryGetValue(poolKey, out Stack<Entity>? entities))
        {
            entities = new Stack<Entity>();
            pooledEntities[poolKey] = entities;
        }

        entities.Push(entity);
        pooledCount++;
    }

    private static void ResetEntity(Entity entity)
    {
        entity.Scene = null;
        entity.Transform.Position = Vector3.Zero;
        entity.Transform.RotationEulerXYZ = Vector3.Zero;
        entity.Transform.Scale = Vector3.One;
        entity.Transform.Parent = null;

        ResetChildren(entity);
    }

    private static void ResetChildren(Entity parent)
    {
        foreach (Entity child in parent.GetChildren())
        {
            child.Transform.Position = Vector3.Zero;
            child.Transform.RotationEulerXYZ = Vector3.Zero;
            child.Transform.Scale = Vector3.One;

            if (child.Get<ModelComponent>() is ModelComponent modelComponent)
            {
                modelComponent.Enabled = true;
            }

            ResetChildren(child);
        }
    }
}
