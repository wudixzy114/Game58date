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
        RegisterAlias("env/reed_patch_near", "Env/Prefabs/ReedPatch", EnvironmentMaterialKind.Reed);
        RegisterAlias("env/reed_patch_mid", "Env/Prefabs/ReedPatch", EnvironmentMaterialKind.Reed);
        RegisterAlias("env/wetland_tree_near", "Env/Prefabs/WetlandTree", EnvironmentMaterialKind.Bark);
        RegisterAlias("env/wetland_reed_mid", "Env/Prefabs/ReedPatch", EnvironmentMaterialKind.Reed);
        RegisterAlias("env/broadleaftree", "Env/Prefabs/BroadleafTree", EnvironmentMaterialKind.Bark);
        RegisterAlias("env/plainsbroadleaf", "Env/Prefabs/BroadleafTree", EnvironmentMaterialKind.Bark);
        RegisterAlias("env/woodlandbushmid", "Env/Prefabs/Bush", EnvironmentMaterialKind.Leaf);
        RegisterAlias("env/plainsbushmid", "Env/Prefabs/Bush", EnvironmentMaterialKind.Leaf);
        RegisterAlias("env/highlandpine", "Env/Prefabs/PineTree", EnvironmentMaterialKind.Bark);
        RegisterAlias("env/alpinepine", "Env/Prefabs/PineTree", EnvironmentMaterialKind.Bark);
        RegisterAlias("env/mountainpinemid", "Env/Prefabs/PineTree", EnvironmentMaterialKind.Bark);
        RegisterAlias("env/highlandrockmid", "Env/Prefabs/RockCluster", EnvironmentMaterialKind.Stone);
        RegisterAlias("env/screerock", "Env/Prefabs/RockCluster", EnvironmentMaterialKind.Stone);
        RegisterAlias("env/screerockmid", "Env/Prefabs/RockCluster", EnvironmentMaterialKind.Stone);
        RegisterAlias("env/alpinerockmid", "Env/Prefabs/RockCluster", EnvironmentMaterialKind.Stone);
        RegisterAlias("env/mountainrock", "Env/Prefabs/RockCluster", EnvironmentMaterialKind.Stone);
        RegisterAlias("env/coastalruin", "Env/Prefabs/RuinArch", EnvironmentMaterialKind.RuinStone);
        RegisterAlias("env/coastalmarkermid", "Env/Prefabs/Cairn", EnvironmentMaterialKind.RuinStone);
        RegisterAlias("env/uplandcairn", "Env/Prefabs/Cairn", EnvironmentMaterialKind.RuinStone);
        RegisterAlias("env/uplandcairnmid", "Env/Prefabs/Cairn", EnvironmentMaterialKind.RuinStone);
        RegisterAlias("env/alpinecairn", "Env/Prefabs/Cairn", EnvironmentMaterialKind.RuinStone);
        RegisterAlias("env/alpinecairnmid", "Env/Prefabs/Cairn", EnvironmentMaterialKind.RuinStone);
        RegisterAlias("env/shoregull", "Env/Prefabs/Gull", EnvironmentMaterialKind.Gull);
        RegisterAlias("env/shoregullmid", "Env/Prefabs/Gull", EnvironmentMaterialKind.Gull);
        RegisterAlias("env/plainsdeer", "Env/Prefabs/Deer", EnvironmentMaterialKind.Deer);
        RegisterAlias("env/plainsdeermid", "Env/Prefabs/Deer", EnvironmentMaterialKind.Deer);
        RegisterAlias("env/mountaingoat", "Env/Prefabs/Goat", EnvironmentMaterialKind.Goat);
        RegisterAlias("env/mountaingoatmid", "Env/Prefabs/Goat", EnvironmentMaterialKind.Goat);
    }

    private void RegisterAlias(string key, string assetKey, EnvironmentMaterialKind fallbackMaterial)
    {
        Register(new EnvironmentAssetDescriptor(key, fallbackMaterial, true));
        Register(new EnvironmentAssetDescriptor(assetKey, fallbackMaterial, false));
        assets[key] = new EnvironmentAssetDescriptor(assetKey, fallbackMaterial, false);
    }
}
