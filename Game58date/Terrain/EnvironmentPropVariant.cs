#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public readonly record struct EnvironmentPropVariant(
    EnvironmentPropKind Kind,
    EnvironmentLodLevel Lod,
    string Name,
    EnvironmentAssetDescriptor Asset,
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
    float MotionSpeed)
{
    public EnvironmentPropVariant(
        EnvironmentPropKind kind,
        EnvironmentLodLevel lod,
        string name,
        float radius,
        float heightScale,
        float densityScale,
        bool requiresSoftGround,
        bool requiresOpenSky,
        float minSlope,
        float maxSlope,
        float minTemperature,
        float maxTemperature,
        float minMoisture,
        float maxMoisture,
        Vector3 positionJitter,
        Vector3 rotationAmplitude,
        Vector3 positionAmplitude,
        float motionSpeed)
        : this(
            kind,
            lod,
            name,
            new EnvironmentAssetDescriptor($"env/{name.ToLowerInvariant()}", ResolveFallbackMaterial(kind)),
            radius,
            heightScale,
            densityScale,
            requiresSoftGround,
            requiresOpenSky,
            minSlope,
            maxSlope,
            minTemperature,
            maxTemperature,
            minMoisture,
            maxMoisture,
            positionJitter,
            rotationAmplitude,
            positionAmplitude,
            motionSpeed)
    {
    }

    private static EnvironmentMaterialKind ResolveFallbackMaterial(EnvironmentPropKind kind)
    {
        return kind switch
        {
            EnvironmentPropKind.BroadleafTree => EnvironmentMaterialKind.Bark,
            EnvironmentPropKind.PineTree => EnvironmentMaterialKind.Bark,
            EnvironmentPropKind.WetlandTree => EnvironmentMaterialKind.Bark,
            EnvironmentPropKind.Bush => EnvironmentMaterialKind.Leaf,
            EnvironmentPropKind.ReedPatch => EnvironmentMaterialKind.Reed,
            EnvironmentPropKind.RockCluster => EnvironmentMaterialKind.Stone,
            EnvironmentPropKind.Cairn => EnvironmentMaterialKind.RuinStone,
            EnvironmentPropKind.RuinArch => EnvironmentMaterialKind.RuinStone,
            EnvironmentPropKind.Deer => EnvironmentMaterialKind.Deer,
            EnvironmentPropKind.Goat => EnvironmentMaterialKind.Goat,
            EnvironmentPropKind.Gull => EnvironmentMaterialKind.Gull,
            _ => EnvironmentMaterialKind.Stone,
        };
    }
}
