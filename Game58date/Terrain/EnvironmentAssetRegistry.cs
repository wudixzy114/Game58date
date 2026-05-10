#nullable enable
using System.Collections.Generic;

namespace Game58date.Terrain;

public sealed class EnvironmentAssetRegistry
{
    private readonly Dictionary<string, EnvironmentAssetDescriptor> assets = new();

    public EnvironmentAssetRegistry()
    {
        RegisterDefaults();
    }

    public EnvironmentAssetDescriptor Resolve(EnvironmentAssetDescriptor descriptor)
    {
        return assets.TryGetValue(descriptor.AssetKey, out EnvironmentAssetDescriptor registered)
            ? registered
            : descriptor;
    }

    public void Register(EnvironmentAssetDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.AssetKey))
        {
            return;
        }

        assets[descriptor.AssetKey] = descriptor;
    }

    private void RegisterDefaults()
    {
        Register(new EnvironmentAssetDescriptor("env/reed_patch_near", EnvironmentMaterialKind.Reed, true));
        Register(new EnvironmentAssetDescriptor("env/reed_patch_mid", EnvironmentMaterialKind.Reed, true));
        Register(new EnvironmentAssetDescriptor("env/wetland_tree_near", EnvironmentMaterialKind.Bark, true));
        Register(new EnvironmentAssetDescriptor("env/wetland_reed_mid", EnvironmentMaterialKind.Reed, true));
    }
}
