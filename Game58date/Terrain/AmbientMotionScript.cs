#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class AmbientMotionScript : SyncScript
{
    private Vector3 basePosition;
    private Vector3 baseRotationEuler;
    private float elapsedSeconds;

    public Vector3 PositionAmplitude { get; set; } = Vector3.Zero;

    public Vector3 RotationAmplitudeEuler { get; set; } = Vector3.Zero;

    public float Speed { get; set; } = 1f;

    public float Phase { get; set; }

    public override void Start()
    {
        RebindBaseTransform();
    }

    public override void Update()
    {
        elapsedSeconds += (float)Game.UpdateTime.Elapsed.TotalSeconds;

        float primaryWave = MathF.Sin(elapsedSeconds * Speed + Phase);
        float secondaryWave = MathF.Cos(elapsedSeconds * (Speed * 0.73f) + Phase * 1.37f);

        Entity.Transform.Position = basePosition + new Vector3(
            PositionAmplitude.X * primaryWave,
            PositionAmplitude.Y * secondaryWave,
            PositionAmplitude.Z * MathF.Sin(elapsedSeconds * (Speed * 0.61f) + Phase * 0.77f));

        Entity.Transform.RotationEulerXYZ = baseRotationEuler + new Vector3(
            RotationAmplitudeEuler.X * secondaryWave,
            RotationAmplitudeEuler.Y * primaryWave,
            RotationAmplitudeEuler.Z * MathF.Sin(elapsedSeconds * (Speed * 0.49f) + Phase));
    }

    public void RebindBaseTransform()
    {
        basePosition = Entity.Transform.Position;
        baseRotationEuler = Entity.Transform.RotationEulerXYZ;
        elapsedSeconds = 0f;
    }
}
