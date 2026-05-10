#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public readonly record struct EnvironmentPropVariant(
    EnvironmentPropKind Kind,
    EnvironmentLodLevel Lod,
    string Name,
    float Radius,
    float HeightScale,
    float DensityScale,
    bool RequiresSoftGround,
    bool RequiresOpenSky,
    float MinSlope,
    float MaxSlope,
    float MinTemperature,
    float MaxTemperature,
    float MinMoisture,
    float MaxMoisture,
    Vector3 PositionJitter,
    Vector3 RotationAmplitude,
    Vector3 PositionAmplitude,
    float MotionSpeed);
