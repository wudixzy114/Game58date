#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Game58date.Terrain;

public sealed class TerrainSceneBootstrapper
{
    public Entity EnsureCamera(Scene scene)
    {
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name == "Camera")
            {
                return entity;
            }
        }

        var cameraEntity = new Entity("RuntimeCamera");
        cameraEntity.Transform.Position = new Vector3(24f, 34f, -24f);
        cameraEntity.Transform.RotationEulerXYZ = new Vector3(0.55f, 0.75f, 0f);
        cameraEntity.Add(new CameraComponent());
        cameraEntity.Add(new BasicCameraController
        {
            KeyboardMovementSpeed = new Vector3(18f, 18f, 18f),
            SpeedFactor = 4f,
        });

        scene.Entities.Add(cameraEntity);
        return cameraEntity;
    }

    public void EnsureTerrainLighting(Scene scene)
    {
        Entity? lightEntity = null;
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name == "Directional light")
            {
                lightEntity = entity;
                break;
            }
        }

        if (lightEntity is null)
        {
            lightEntity = new Entity("Directional light");
            lightEntity.Add(new LightComponent
            {
                Type = new LightDirectional(),
                Intensity = 14f,
            });
            scene.Entities.Add(lightEntity);
        }

        LightComponent? lightComponent = lightEntity.Get<LightComponent>();
        if (lightComponent is null)
        {
            lightComponent = new LightComponent
            {
                Type = new LightDirectional(),
                Intensity = 14f,
            };
            lightEntity.Add(lightComponent);
        }

        lightComponent.Intensity = 14f;
        lightComponent.SetColor(new Color3(1.0f, 0.98f, 0.94f));

        Vector3 desiredLightDirection = Vector3.Normalize(new Vector3(-0.35f, -1.0f, -0.25f));
        lightEntity.Transform.Rotation = Quaternion.BetweenDirections(-Vector3.UnitZ, desiredLightDirection);
    }

    public void DisableLegacyEntities(Scene scene)
    {
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name is not ("Ground" or "Sphere"))
            {
                continue;
            }

            ModelComponent? model = entity.Get<ModelComponent>();
            if (model is not null)
            {
                model.Enabled = false;
            }
        }
    }
}
