#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;

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
