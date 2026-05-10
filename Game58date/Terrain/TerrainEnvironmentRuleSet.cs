#nullable enable
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class TerrainEnvironmentRuleSet
{
    private readonly List<EnvironmentSpawnRule> vegetationRules;
    private readonly List<EnvironmentSpawnRule> structureRules;
    private readonly List<EnvironmentSpawnRule> animalRules;

    public TerrainEnvironmentRuleSet(
        List<EnvironmentSpawnRule>? vegetationRules = null,
        List<EnvironmentSpawnRule>? structureRules = null,
        List<EnvironmentSpawnRule>? animalRules = null)
    {
        this.vegetationRules = vegetationRules ?? new List<EnvironmentSpawnRule>();
        this.structureRules = structureRules ?? new List<EnvironmentSpawnRule>();
        this.animalRules = animalRules ?? new List<EnvironmentSpawnRule>();
    }

    public IReadOnlyList<EnvironmentSpawnRule> VegetationRules => vegetationRules;

    public IReadOnlyList<EnvironmentSpawnRule> StructureRules => structureRules;

    public IReadOnlyList<EnvironmentSpawnRule> AnimalRules => animalRules;

    public bool HasAnyRules => vegetationRules.Count > 0 || structureRules.Count > 0 || animalRules.Count > 0;

    public EnvironmentRuleSetConfig ToConfig()
    {
        return new EnvironmentRuleSetConfig
        {
            VegetationRules = ConvertRules(vegetationRules),
            StructureRules = ConvertRules(structureRules),
            AnimalRules = ConvertRules(animalRules),
        };
    }

    public static TerrainEnvironmentRuleSet CreateDefault()
    {
        return new TerrainEnvironmentRuleSet(
            BuildVegetationRules(),
            BuildStructureRules(),
            BuildAnimalRules());
    }

    public static TerrainEnvironmentRuleSet FromConfig(EnvironmentRuleSetConfig config)
    {
        return new TerrainEnvironmentRuleSet(
            ConvertRules(config.VegetationRules),
            ConvertRules(config.StructureRules),
            ConvertRules(config.AnimalRules));
    }

    private static List<EnvironmentSpawnRuleConfig> ConvertRules(IReadOnlyList<EnvironmentSpawnRule> rules)
    {
        var configs = new List<EnvironmentSpawnRuleConfig>(rules.Count);
        foreach (EnvironmentSpawnRule rule in rules)
        {
            configs.Add(new EnvironmentSpawnRuleConfig
            {
                Name = rule.Name,
                PrimaryBiome = rule.PrimaryBiome,
                Weight = rule.Weight,
                ProbabilityThreshold = rule.ProbabilityThreshold,
                MaxPlacementsPerChunk = rule.MaxPlacementsPerChunk,
                GridStep = rule.GridStep,
                MinEdgePadding = rule.MinEdgePadding,
                ClearanceHeight = rule.ClearanceHeight,
                MinSlope = rule.MinSlope,
                MaxSlope = rule.MaxSlope,
                MinMoisture = rule.MinMoisture,
                MaxMoisture = rule.MaxMoisture,
                MinTemperature = rule.MinTemperature,
                MaxTemperature = rule.MaxTemperature,
                RequiresSoftGround = rule.RequiresSoftGround,
                AllowsWaterAdjacency = rule.AllowsWaterAdjacency,
                NearVariant = ConvertVariant(rule.NearVariant),
                MidVariant = ConvertVariant(rule.MidVariant),
                FarVariant = rule.FarVariant is { } farVariant ? ConvertVariant(farVariant) : null,
            });
        }

        return configs;
    }

    private static List<EnvironmentSpawnRule> ConvertRules(IReadOnlyList<EnvironmentSpawnRuleConfig> configs)
    {
        var rules = new List<EnvironmentSpawnRule>(configs.Count);
        foreach (EnvironmentSpawnRuleConfig config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.Name))
            {
                continue;
            }

            rules.Add(new EnvironmentSpawnRule(
                config.Name,
                config.PrimaryBiome,
                config.Weight,
                config.ProbabilityThreshold,
                config.MaxPlacementsPerChunk,
                config.GridStep,
                config.MinEdgePadding,
                config.ClearanceHeight,
                config.MinSlope,
                config.MaxSlope,
                config.MinMoisture,
                config.MaxMoisture,
                config.MinTemperature,
                config.MaxTemperature,
                config.RequiresSoftGround,
                config.AllowsWaterAdjacency,
                ConvertVariant(config.NearVariant),
                ConvertVariant(config.MidVariant),
                config.FarVariant is not null ? ConvertVariant(config.FarVariant) : null));
        }

        return rules;
    }

    private static EnvironmentPropVariantConfig ConvertVariant(EnvironmentPropVariant variant)
    {
        return new EnvironmentPropVariantConfig
        {
            Kind = variant.Kind,
            Lod = variant.Lod,
            Name = variant.Name,
            AssetKey = variant.Asset.AssetKey,
            FallbackMaterial = variant.Asset.FallbackMaterial,
            UsesPlaceholderGeometry = variant.Asset.UsesPlaceholderGeometry,
            Radius = variant.Radius,
            HeightScale = variant.HeightScale,
            DensityScale = variant.DensityScale,
            RequiresSoftGround = variant.RequiresSoftGround,
            RequiresOpenSky = variant.RequiresOpenSky,
            MinSlope = variant.MinSlope,
            MaxSlope = variant.MaxSlope,
            MinTemperature = variant.MinTemperature,
            MaxTemperature = variant.MaxTemperature,
            MinMoisture = variant.MinMoisture,
            MaxMoisture = variant.MaxMoisture,
            PositionJitter = variant.PositionJitter,
            RotationAmplitude = variant.RotationAmplitude,
            PositionAmplitude = variant.PositionAmplitude,
            MotionSpeed = variant.MotionSpeed,
        };
    }

    private static EnvironmentPropVariant ConvertVariant(EnvironmentPropVariantConfig config)
    {
        string assetKey = string.IsNullOrWhiteSpace(config.AssetKey)
            ? $"env/{config.Name.ToLowerInvariant()}"
            : config.AssetKey;
        EnvironmentMaterialKind fallbackMaterial = config.FallbackMaterial ?? ResolveFallbackMaterial(config.Kind);
        bool usesPlaceholderGeometry = config.UsesPlaceholderGeometry ?? true;
        return new EnvironmentPropVariant(
            config.Kind,
            config.Lod,
            config.Name,
            new EnvironmentAssetDescriptor(assetKey, fallbackMaterial, usesPlaceholderGeometry),
            config.Radius,
            config.HeightScale,
            config.DensityScale,
            config.RequiresSoftGround,
            config.RequiresOpenSky,
            config.MinSlope,
            config.MaxSlope,
            config.MinTemperature,
            config.MaxTemperature,
            config.MinMoisture,
            config.MaxMoisture,
            config.PositionJitter,
            config.RotationAmplitude,
            config.PositionAmplitude,
            config.MotionSpeed);
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

    private static List<EnvironmentSpawnRule> BuildVegetationRules()
    {
        return new List<EnvironmentSpawnRule>
        {
            new(
                "shore_reeds",
                BiomeKind.Shore,
                1.0f,
                0.55f,
                4,
                4,
                2,
                5,
                0f,
                0.26f,
                0.35f,
                1f,
                0f,
                1f,
                false,
                true,
                new EnvironmentPropVariant(EnvironmentPropKind.ReedPatch, EnvironmentLodLevel.Near, "ReedPatch", new EnvironmentAssetDescriptor("env/reed_patch_near", EnvironmentMaterialKind.Reed), 1.1f, 1.0f, 1.0f, false, false, 0f, 0.32f, 0f, 1f, 0.35f, 1f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.02f, 0.11f, 0.03f), new Vector3(0f, 0.01f, 0f), 1.35f),
                new EnvironmentPropVariant(EnvironmentPropKind.ReedPatch, EnvironmentLodLevel.Mid, "ReedPatchMid", new EnvironmentAssetDescriptor("env/reed_patch_mid", EnvironmentMaterialKind.Reed), 0.95f, 0.75f, 0.75f, false, false, 0f, 0.32f, 0f, 1f, 0.35f, 1f, new Vector3(0.12f, 0f, 0.12f), new Vector3(0.01f, 0.07f, 0.02f), new Vector3(0f, 0.008f, 0f), 1.0f)),
            new(
                "wetland_growth",
                BiomeKind.Wetland,
                1.0f,
                0.46f,
                5,
                4,
                2,
                5,
                0f,
                0.24f,
                0.60f,
                1f,
                0.18f,
                0.95f,
                false,
                true,
                new EnvironmentPropVariant(EnvironmentPropKind.WetlandTree, EnvironmentLodLevel.Near, "WetlandTree", new EnvironmentAssetDescriptor("env/wetland_tree_near", EnvironmentMaterialKind.Bark), 1.8f, 1.0f, 0.65f, false, false, 0f, 0.24f, 0.18f, 0.95f, 0.60f, 1f, new Vector3(0.20f, 0f, 0.20f), new Vector3(0.02f, 0.09f, 0.02f), new Vector3(0f, 0.05f, 0f), 0.96f),
                new EnvironmentPropVariant(EnvironmentPropKind.ReedPatch, EnvironmentLodLevel.Mid, "WetlandReedMid", new EnvironmentAssetDescriptor("env/wetland_reed_mid", EnvironmentMaterialKind.Reed), 1.0f, 0.85f, 1.0f, false, false, 0f, 0.24f, 0.18f, 0.95f, 0.60f, 1f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.01f, 0.07f, 0.02f), new Vector3(0f, 0.01f, 0f), 1.10f)),
            new(
                "woodland_canopy",
                BiomeKind.Woodland,
                1.0f,
                0.56f,
                4,
                4,
                2,
                6,
                0f,
                0.40f,
                0.38f,
                1f,
                0.28f,
                0.95f,
                true,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.BroadleafTree, EnvironmentLodLevel.Near, "BroadleafTree", 2.2f, 1.0f, 1.0f, true, false, 0f, 0.40f, 0.28f, 0.95f, 0.38f, 1f, new Vector3(0.20f, 0f, 0.20f), new Vector3(0.02f, 0.08f, 0.02f), new Vector3(0f, 0.04f, 0f), 0.85f),
                new EnvironmentPropVariant(EnvironmentPropKind.Bush, EnvironmentLodLevel.Mid, "WoodlandBushMid", 1.2f, 0.85f, 1.0f, true, false, 0f, 0.40f, 0.28f, 0.95f, 0.38f, 1f, new Vector3(0.16f, 0f, 0.16f), new Vector3(0.01f, 0.05f, 0.02f), new Vector3(0f, 0.015f, 0f), 0.95f)),
            new(
                "plains_growth",
                BiomeKind.Plains,
                1.0f,
                0.78f,
                3,
                4,
                2,
                5,
                0f,
                0.34f,
                0.22f,
                0.88f,
                0.24f,
                1f,
                true,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.BroadleafTree, EnvironmentLodLevel.Near, "PlainsBroadleaf", 2.0f, 0.95f, 1.0f, true, false, 0f, 0.34f, 0.24f, 1f, 0.22f, 0.88f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.02f, 0.08f, 0.02f), new Vector3(0f, 0.04f, 0f), 0.80f),
                new EnvironmentPropVariant(EnvironmentPropKind.Bush, EnvironmentLodLevel.Mid, "PlainsBushMid", 1.1f, 0.80f, 0.90f, true, false, 0f, 0.34f, 0.24f, 1f, 0.22f, 0.88f, new Vector3(0.14f, 0f, 0.14f), new Vector3(0.01f, 0.05f, 0.02f), new Vector3(0f, 0.015f, 0f), 1.0f)),
            new(
                "highland_growth",
                BiomeKind.Hills,
                1.0f,
                0.66f,
                4,
                4,
                2,
                5,
                0.12f,
                0.48f,
                0.16f,
                0.80f,
                0.18f,
                0.82f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.PineTree, EnvironmentLodLevel.Near, "HighlandPine", 1.8f, 1.0f, 0.9f, false, false, 0.12f, 0.48f, 0.18f, 0.82f, 0.16f, 0.80f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.01f, 0.05f, 0.01f), new Vector3(0f, 0.03f, 0f), 0.70f),
                new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Mid, "HighlandRockMid", 1.0f, 0.80f, 0.95f, false, false, 0.12f, 0.48f, 0.18f, 0.82f, 0.16f, 0.80f, new Vector3(0.16f, 0f, 0.16f), Vector3.Zero, Vector3.Zero, 0f)),
            new(
                "scree_rocks",
                BiomeKind.Scree,
                1.0f,
                0.42f,
                5,
                4,
                2,
                4,
                0.18f,
                0.82f,
                0f,
                0.58f,
                0f,
                0.74f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Near, "ScreeRock", 1.15f, 1.0f, 1.0f, false, false, 0.18f, 0.82f, 0f, 0.74f, 0f, 0.58f, new Vector3(0.12f, 0f, 0.12f), Vector3.Zero, Vector3.Zero, 0f),
                new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Mid, "ScreeRockMid", 0.95f, 0.70f, 0.90f, false, false, 0.18f, 0.82f, 0f, 0.74f, 0f, 0.58f, new Vector3(0.08f, 0f, 0.08f), Vector3.Zero, Vector3.Zero, 0f)),
            new(
                "alpine_sparse",
                BiomeKind.Alpine,
                1.0f,
                0.50f,
                4,
                4,
                2,
                5,
                0.08f,
                0.42f,
                0.10f,
                0.70f,
                0f,
                0.48f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.PineTree, EnvironmentLodLevel.Near, "AlpinePine", 1.7f, 0.88f, 0.75f, false, false, 0.08f, 0.42f, 0f, 0.48f, 0.10f, 0.70f, new Vector3(0.16f, 0f, 0.16f), new Vector3(0.01f, 0.04f, 0.01f), new Vector3(0f, 0.02f, 0f), 0.62f),
                new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Mid, "AlpineRockMid", 0.95f, 0.82f, 1.0f, false, false, 0.08f, 0.42f, 0f, 0.48f, 0.10f, 0.70f, new Vector3(0.10f, 0f, 0.10f), Vector3.Zero, Vector3.Zero, 0f)),
            new(
                "mountain_rocks",
                BiomeKind.Mountains,
                1.0f,
                0.54f,
                4,
                4,
                2,
                5,
                0.14f,
                0.86f,
                0f,
                0.68f,
                0f,
                0.62f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Near, "MountainRock", 1.25f, 1.0f, 1.0f, false, false, 0.14f, 0.86f, 0f, 0.62f, 0f, 0.68f, new Vector3(0.12f, 0f, 0.12f), Vector3.Zero, Vector3.Zero, 0f),
                new EnvironmentPropVariant(EnvironmentPropKind.PineTree, EnvironmentLodLevel.Mid, "MountainPineMid", 1.5f, 0.74f, 0.60f, false, false, 0.14f, 0.40f, 0f, 0.50f, 0.10f, 0.58f, new Vector3(0.14f, 0f, 0.14f), new Vector3(0.01f, 0.04f, 0.01f), new Vector3(0f, 0.02f, 0f), 0.55f)),
        };
    }

    private static List<EnvironmentSpawnRule> BuildStructureRules()
    {
        return new List<EnvironmentSpawnRule>
        {
            new(
                "coastal_ruin",
                BiomeKind.Shore,
                1.0f,
                0.78f,
                1,
                6,
                2,
                6,
                0f,
                0.22f,
                0f,
                1f,
                0f,
                1f,
                false,
                true,
                new EnvironmentPropVariant(EnvironmentPropKind.RuinArch, EnvironmentLodLevel.Near, "CoastalRuin", 2.1f, 1.0f, 1.0f, false, false, 0f, 0.22f, 0f, 1f, 0f, 1f, new Vector3(0.08f, 0f, 0.08f), Vector3.Zero, Vector3.Zero, 0f),
                new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Mid, "CoastalMarkerMid", 1.0f, 0.88f, 0.50f, false, false, 0f, 0.22f, 0f, 1f, 0f, 1f, new Vector3(0.06f, 0f, 0.06f), Vector3.Zero, Vector3.Zero, 0f)),
            new(
                "upland_marker",
                BiomeKind.Hills,
                1.0f,
                0.76f,
                1,
                6,
                2,
                6,
                0f,
                0.48f,
                0f,
                1f,
                0f,
                1f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Near, "UplandCairn", 1.2f, 1.0f, 1.0f, false, false, 0f, 0.48f, 0f, 1f, 0f, 1f, new Vector3(0.06f, 0f, 0.06f), Vector3.Zero, Vector3.Zero, 0f),
                new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Mid, "UplandCairnMid", 0.95f, 0.70f, 0.55f, false, false, 0f, 0.48f, 0f, 1f, 0f, 1f, new Vector3(0.04f, 0f, 0.04f), Vector3.Zero, Vector3.Zero, 0f)),
            new(
                "alpine_marker",
                BiomeKind.Alpine,
                1.0f,
                0.72f,
                1,
                6,
                2,
                6,
                0f,
                0.52f,
                0f,
                1f,
                0f,
                1f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Near, "AlpineCairn", 1.2f, 1.0f, 1.0f, false, false, 0f, 0.52f, 0f, 1f, 0f, 1f, new Vector3(0.05f, 0f, 0.05f), Vector3.Zero, Vector3.Zero, 0f),
                new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Mid, "AlpineCairnMid", 0.90f, 0.68f, 0.55f, false, false, 0f, 0.52f, 0f, 1f, 0f, 1f, new Vector3(0.04f, 0f, 0.04f), Vector3.Zero, Vector3.Zero, 0f)),
        };
    }

    private static List<EnvironmentSpawnRule> BuildAnimalRules()
    {
        return new List<EnvironmentSpawnRule>
        {
            new(
                "shore_gulls",
                BiomeKind.Shore,
                1.0f,
                0.70f,
                1,
                4,
                2,
                4,
                0f,
                0.35f,
                0f,
                1f,
                0f,
                1f,
                false,
                true,
                new EnvironmentPropVariant(EnvironmentPropKind.Gull, EnvironmentLodLevel.Near, "ShoreGull", 1.1f, 1.0f, 1.0f, false, true, 0f, 0.35f, 0f, 1f, 0f, 1f, new Vector3(0.10f, 0f, 0.10f), new Vector3(0.05f, 0.18f, 0.05f), new Vector3(0f, 0.12f, 0f), 1.55f),
                new EnvironmentPropVariant(EnvironmentPropKind.Gull, EnvironmentLodLevel.Mid, "ShoreGullMid", 0.72f, 0.70f, 0.60f, false, true, 0f, 0.35f, 0f, 1f, 0f, 1f, new Vector3(0.08f, 0f, 0.08f), new Vector3(0.03f, 0.12f, 0.03f), new Vector3(0f, 0.08f, 0f), 1.1f)),
            new(
                "upland_deer",
                BiomeKind.Plains,
                1.0f,
                0.72f,
                1,
                4,
                2,
                4,
                0f,
                0.35f,
                0.10f,
                0.90f,
                0.20f,
                1f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.Deer, EnvironmentLodLevel.Near, "PlainsDeer", 1.4f, 1.0f, 1.0f, false, false, 0f, 0.35f, 0.20f, 1f, 0.10f, 0.90f, new Vector3(0.10f, 0f, 0.10f), new Vector3(0.02f, 0.10f, 0.01f), new Vector3(0f, 0.03f, 0f), 0.92f),
                new EnvironmentPropVariant(EnvironmentPropKind.Deer, EnvironmentLodLevel.Mid, "PlainsDeerMid", 1.0f, 0.72f, 0.55f, false, false, 0f, 0.35f, 0.20f, 1f, 0.10f, 0.90f, new Vector3(0.08f, 0f, 0.08f), new Vector3(0.01f, 0.06f, 0.01f), new Vector3(0f, 0.02f, 0f), 0.70f)),
            new(
                "mountain_goats",
                BiomeKind.Mountains,
                1.0f,
                0.68f,
                1,
                4,
                2,
                4,
                0f,
                0.40f,
                0f,
                0.80f,
                0f,
                0.70f,
                false,
                false,
                new EnvironmentPropVariant(EnvironmentPropKind.Goat, EnvironmentLodLevel.Near, "MountainGoat", 1.2f, 1.0f, 1.0f, false, false, 0f, 0.40f, 0f, 0.70f, 0f, 0.80f, new Vector3(0.10f, 0f, 0.10f), new Vector3(0.03f, 0.12f, 0.01f), new Vector3(0f, 0.02f, 0f), 1.05f),
                new EnvironmentPropVariant(EnvironmentPropKind.Goat, EnvironmentLodLevel.Mid, "MountainGoatMid", 0.86f, 0.72f, 0.58f, false, false, 0f, 0.40f, 0f, 0.70f, 0f, 0.80f, new Vector3(0.08f, 0f, 0.08f), new Vector3(0.02f, 0.07f, 0.01f), new Vector3(0f, 0.015f, 0f), 0.72f)),
        };
    }
}
