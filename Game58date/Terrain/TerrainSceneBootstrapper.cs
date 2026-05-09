#nullable enable
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Game58date.Terrain;

public sealed class TerrainSceneBootstrapper
{
    public const float PlayerRadius = FirstPersonCharacterController.DefaultRadius;
    public const float PlayerEyeHeightFromFeet = FirstPersonCharacterController.DefaultEyeHeight;
    public const float PlayerHeadHeightAboveEye = FirstPersonCharacterController.DefaultHeadHeightAboveEye;

    public Entity EnsureCamera(Scene scene)
    {
        Entity? cameraEntity = null;
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name == "Camera")
            {
                cameraEntity = entity;
                break;
            }
        }

        if (cameraEntity is null)
        {
            cameraEntity = new Entity("Camera");
            cameraEntity.Transform.Position = new Vector3(24f, 34f, -24f);
            cameraEntity.Transform.RotationEulerXYZ = new Vector3(0.55f, 0.75f, 0f);
            scene.Entities.Add(cameraEntity);
        }

        if (cameraEntity.Get<CameraComponent>() is null)
        {
            cameraEntity.Add(new CameraComponent());
        }

        CameraComponent camera = cameraEntity.Get<CameraComponent>()!;
        camera.NearClipPlane = 0.03f;
        camera.FarClipPlane = 1200f;
        camera.VerticalFieldOfView = 70f;

        return cameraEntity;
    }

    public FirstPersonCharacterController EnsureFirstPersonController(Entity cameraEntity, VoxelTerrainWorldRuntime runtime)
    {
        FirstPersonCharacterController? controller = cameraEntity.Get<FirstPersonCharacterController>();
        if (controller is not null)
        {
            controller.Initialize(runtime);
            return controller;
        }

        controller = new FirstPersonCharacterController
        {
            Radius = PlayerRadius,
            EyeHeightFromFeet = PlayerEyeHeightFromFeet,
            HeadHeightAboveEye = PlayerHeadHeightAboveEye,
        };
        controller.Initialize(runtime);
        cameraEntity.Add(controller);
        return controller;
    }

    public Entity EnsureTerrainLighting(Scene scene)
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
        return lightEntity;
    }

    public void PruneLegacySceneEntities(Scene scene)
    {
        var entitiesToRemove = new List<Entity>();
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name is not ("Ground" or "Sphere"))
            {
                continue;
            }

            entitiesToRemove.Add(entity);
        }

        foreach (Entity entity in entitiesToRemove)
        {
            entity.Scene = null;
        }
    }
}
