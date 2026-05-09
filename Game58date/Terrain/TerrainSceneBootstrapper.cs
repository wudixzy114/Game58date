#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Lights;
using Stride.Physics;

namespace Game58date.Terrain;

public sealed class TerrainSceneBootstrapper
{
    public const float PlayerCapsuleRadius = 0.45f;
    public const float PlayerCapsuleLength = 1.0f;
    public const float PlayerHalfHeight = PlayerCapsuleRadius + PlayerCapsuleLength * 0.5f;
    public const float PlayerEyeHeightFromCenter = 0.67f;

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

    public Entity EnsureFirstPersonPlayer(Scene scene, Vector3 spawnPosition, Entity cameraEntity)
    {
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name == "FirstPersonPlayer")
            {
                FirstPersonCharacterController? existingController = entity.Get<FirstPersonCharacterController>();
                if (existingController is not null)
                {
                    existingController.CameraEntity = cameraEntity;
                }

                return entity;
            }
        }

        var playerEntity = new Entity("FirstPersonPlayer");
        playerEntity.Transform.Position = spawnPosition;

        var character = new CharacterComponent
        {
            ColliderShape = new CapsuleColliderShape(false, PlayerCapsuleRadius, PlayerCapsuleLength, ShapeOrientation.UpY),
            StepHeight = 0.45f,
            JumpSpeed = 7.2f,
            MaxSlope = new AngleSingle(47f, AngleType.Degree),
            FallSpeed = 45f,
            Gravity = new Vector3(0f, -24f, 0f),
        };

        var controller = new FirstPersonCharacterController
        {
            CameraEntity = cameraEntity,
            EyeHeight = PlayerEyeHeightFromCenter,
        };

        playerEntity.Add(character);
        playerEntity.Add(controller);
        scene.Entities.Add(playerEntity);
        return playerEntity;
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
