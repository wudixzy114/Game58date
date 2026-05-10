#nullable enable

namespace Game58date.Terrain;

public readonly record struct WeatherEffectDescriptor(
    WeatherKind Weather,
    string ParticleAssetKey,
    EnvironmentMaterialKind FallbackMaterial,
    bool UsesPlaceholderGeometry = true);
