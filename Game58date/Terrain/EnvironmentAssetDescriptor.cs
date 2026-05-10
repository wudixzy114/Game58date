#nullable enable

namespace Game58date.Terrain;

public readonly record struct EnvironmentAssetDescriptor(
    string AssetKey,
    EnvironmentMaterialKind FallbackMaterial,
    bool UsesPlaceholderGeometry = true);
