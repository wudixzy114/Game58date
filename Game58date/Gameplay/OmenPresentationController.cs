#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Lights;

namespace Game58date.Gameplay;

public sealed class OmenPresentationController
{
    private readonly List<OmenMarker> markers = new();
    private readonly Queue<string> presentationLog = new();

    private Model? markerModel;
    private Entity? groundEntity;
    private Entity? focalEntity;

    public IReadOnlyCollection<string> PresentationLog => presentationLog;

    public void Initialize(Scene scene)
    {
        if (scene is null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        groundEntity = FindEntity(scene, "Ground");
        focalEntity = FindEntity(scene, "Sphere");
        markerModel = focalEntity?.Get<ModelComponent>()?.Model;

        if (markers.Count > 0)
        {
            return;
        }

        CreateMarkers(scene);
    }

    public void Update(WorldLawRuntimeState state, float deltaTimeSeconds)
    {
        if (markers.Count == 0)
        {
            return;
        }

        float pulse = state.Perception.IsActive
            ? 0.75f + MathF.Sin(state.WorldTimeSeconds * 7.2f) * 0.25f
            : 0.42f + MathF.Sin(state.WorldTimeSeconds * 4.8f) * 0.18f;

        int highlightedIndex = GetHighlightedMarkerIndex(state.Omen.ActiveOmen?.OmenType ?? state.World.LastOmen);
        float omenScore = state.Omen.ActiveOmen?.Score ?? state.Omen.LastScore;
        float perceptionBoost = state.Perception.IsActive ? state.Perception.Intensity : 0f;

        for (int index = 0; index < markers.Count; index++)
        {
            OmenMarker marker = markers[index];
            bool highlighted = index == highlightedIndex;
            float activity = highlighted ? 1f : 0.18f + omenScore * 0.12f;
            float scale = highlighted
                ? 0.32f + pulse * (0.22f + perceptionBoost * 0.18f)
                : 0.10f + MathF.Sin(state.WorldTimeSeconds + index) * 0.01f + perceptionBoost * 0.03f;

            marker.Entity.Transform.Scale = new Vector3(scale);
            marker.LightComponent.Intensity = highlighted
                ? 5f + pulse * (16f + perceptionBoost * 18f)
                : 0.25f + perceptionBoost * 1.2f;
            marker.Light.Radius = highlighted
                ? 6f + omenScore * 5f + perceptionBoost * 7f
                : 1.8f + perceptionBoost;
            marker.LightComponent.SetColor(GetMarkerColor(index, activity, perceptionBoost));
        }

        UpdateLandmarkPresentation(state, pulse);
    }

    public void HandleOmenActivated(OmenRecord omenRecord)
    {
        EnqueuePresentationLog($"Presentation omen {omenRecord.OmenType} source={omenRecord.Source} score={omenRecord.Score:0.00}");
    }

    public void HandlePerceptionActivated(float intensity)
    {
        EnqueuePresentationLog($"Perception emphasis activated intensity={intensity:0.00}");
    }

    private void UpdateLandmarkPresentation(WorldLawRuntimeState state, float pulse)
    {
        if (focalEntity is not null)
        {
            float scale = 1.00f + state.World.PathVisibility * 0.28f + state.Omen.LastScore * 0.18f + state.Perception.Intensity * 0.22f + pulse * 0.03f;
            focalEntity.Transform.Scale = new Vector3(scale);
            focalEntity.Transform.Position = new Vector3(
                0f,
                0.55f + state.World.BlessingWeight * 0.35f + state.Perception.Intensity * 0.20f,
                0f);
        }

        if (groundEntity is not null)
        {
            float spread = 1.00f + state.World.ResourcePressure * 0.10f + state.Perception.Intensity * 0.05f;
            groundEntity.Transform.Scale = new Vector3(spread, 1.00f, spread);
        }
    }

    private void CreateMarkers(Scene scene)
    {
        var definitions = new[]
        {
            new MarkerDefinition("OmenNature", new Vector3(-9f, 2.1f, 8f)),
            new MarkerDefinition("OmenSociety", new Vector3(-3.8f, 1.8f, 10f)),
            new MarkerDefinition("OmenGuide", new Vector3(0f, 1.7f, 11.5f)),
            new MarkerDefinition("OmenDivination", new Vector3(4.0f, 1.8f, 10f)),
            new MarkerDefinition("OmenPath", new Vector3(9f, 2.1f, 8f)),
        };

        foreach (MarkerDefinition definition in definitions)
        {
            var entity = new Entity(definition.Name)
            {
                Transform =
                {
                    Position = definition.Position,
                    Scale = new Vector3(0.12f),
                }
            };

            if (markerModel is not null)
            {
                entity.Add(new ModelComponent(markerModel));
            }

            var light = new LightComponent
            {
                Type = new LightPoint { Radius = 2f },
                Intensity = 0.25f,
            };

            entity.Add(light);
            scene.Entities.Add(entity);
            markers.Add(new OmenMarker(entity, light, (LightPoint)light.Type));
        }
    }

    private void EnqueuePresentationLog(string message)
    {
        presentationLog.Enqueue(message);
        while (presentationLog.Count > 6)
        {
            presentationLog.Dequeue();
        }
    }

    private static int GetHighlightedMarkerIndex(OmenType omenType)
    {
        return omenType switch
        {
            OmenType.NaturalAnomaly => 0,
            OmenType.SocialShift => 1,
            OmenType.GuideArrival => 2,
            OmenType.Divination => 3,
            OmenType.PathRevelation => 4,
            _ => -1,
        };
    }

    private static Color3 GetMarkerColor(int index, float activity, float perceptionBoost)
    {
        float boost = 0.25f + activity * 0.75f + perceptionBoost * 0.15f;
        return index switch
        {
            0 => new Color3(1.00f * boost, 0.42f * boost, 0.36f * boost),
            1 => new Color3(1.00f * boost, 0.82f * boost, 0.34f * boost),
            2 => new Color3(0.48f * boost, 1.00f * boost, 0.70f * boost),
            3 => new Color3(0.66f * boost, 0.84f * boost, 1.00f * boost),
            _ => new Color3(0.88f * boost, 0.96f * boost, 1.00f * boost),
        };
    }

    private static Entity? FindEntity(Scene scene, string name)
    {
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name == name)
            {
                return entity;
            }
        }

        return null;
    }

    private readonly record struct MarkerDefinition(string Name, Vector3 Position);

    private sealed class OmenMarker
    {
        public OmenMarker(Entity entity, LightComponent lightComponent, LightPoint light)
        {
            Entity = entity;
            LightComponent = lightComponent;
            Light = light;
        }

        public Entity Entity { get; }

        public LightComponent LightComponent { get; }

        public LightPoint Light { get; }
    }
}
