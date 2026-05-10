#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public readonly record struct EnvironmentPlacementRecord(
    EnvironmentPropVariant Variant,
    Vector3 LocalPosition,
    float YawRadians,
    float Variation,
    WorldSample Sample);
