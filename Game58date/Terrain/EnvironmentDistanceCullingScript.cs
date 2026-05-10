#nullable enable
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class EnvironmentDistanceCullingScript : SyncScript
{
    private readonly List<ModelComponent> modelComponents = new();
    private Entity? cameraEntity;
    private bool isVisible = true;

    public float VisibleDistance { get; set; } = 56f;

    public float Hysteresis { get; set; } = 8f;

    public override void Start()
    {
        modelComponents.Clear();
        CollectModelComponents(Entity, modelComponents);
        ApplyVisibility(true);
    }

    public override void Update()
    {
        if (VisibleDistance <= 0f || modelComponents.Count == 0)
        {
            return;
        }

        cameraEntity ??= FindPrimaryCamera(Entity.Scene);
        if (cameraEntity is null)
        {
            return;
        }

        Vector3 cameraPosition = cameraEntity.Transform.WorldMatrix.TranslationVector;
        Vector3 environmentPosition = Entity.Transform.WorldMatrix.TranslationVector;
        float cutoff = isVisible ? VisibleDistance + Hysteresis : VisibleDistance;
        bool shouldBeVisible = Vector3.DistanceSquared(cameraPosition, environmentPosition) <= cutoff * cutoff;
        if (shouldBeVisible == isVisible)
        {
            return;
        }

        ApplyVisibility(shouldBeVisible);
    }

    private void ApplyVisibility(bool visible)
    {
        isVisible = visible;
        foreach (ModelComponent modelComponent in modelComponents)
        {
            modelComponent.Enabled = visible;
        }
    }

    private static void CollectModelComponents(Entity entity, List<ModelComponent> components)
    {
        if (entity.Get<ModelComponent>() is ModelComponent modelComponent)
        {
            components.Add(modelComponent);
        }

        foreach (Entity child in entity.GetChildren())
        {
            CollectModelComponents(child, components);
        }
    }

    private static Entity? FindPrimaryCamera(Scene? scene)
    {
        if (scene is null)
        {
            return null;
        }

        foreach (Entity entity in scene.Entities)
        {
            if (entity.Get<CameraComponent>() is not null)
            {
                return entity;
            }
        }

        return null;
    }
}
