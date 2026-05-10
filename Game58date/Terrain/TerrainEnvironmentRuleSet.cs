#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class TerrainEnvironmentRuleSet
{
    private readonly List<EnvironmentSpawnRule> vegetationRules;
    private readonly List<EnvironmentSpawnRule> structureRules;
    private readonly List<EnvironmentSpawnRule> animalRules;

    public TerrainEnvironmentRuleSet()
    {
        vegetationRules = BuildVegetationRules();
        structureRules = BuildStructureRules();
        animalRules = BuildAnimalRules();
    }

    public IReadOnlyList<EnvironmentSpawnRule> VegetationRules => vegetationRules;

    public IReadOnlyList<EnvironmentSpawnRule> StructureRules => structureRules;

    public IReadOnlyList<EnvironmentSpawnRule> AnimalRules => animalRules;

    private static List<EnvironmentSpawnRule> BuildVegetationRules()
    {
        return new List<EnvironmentSpawnRule>
        {
            new(
                "shore_reeds",
                BiomeKind.Shore,
                Weight: 1.0f,
                ProbabilityThreshold: 0.55f,
                MaxPlacementsPerChunk: 4,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 5,
                MinSlope: 0f,
                MaxSlope: 0.26f,
                MinMoisture: 0.35f,
                MaxMoisture: 1f,
                MinTemperature: 0f,
                MaxTemperature: 1f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: true,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.ReedPatch, EnvironmentLodLevel.Near, "ReedPatch", new EnvironmentAssetDescriptor("env/reed_patch_near", EnvironmentMaterialKind.Reed), 1.1f, 1.0f, 1.0f, false, false, 0f, 0.32f, 0f, 1f, 0.35f, 1f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.02f, 0.11f, 0.03f), new Vector3(0f, 0.01f, 0f), 1.35f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.ReedPatch, EnvironmentLodLevel.Mid, "ReedPatchMid", new EnvironmentAssetDescriptor("env/reed_patch_mid", EnvironmentMaterialKind.Reed), 0.95f, 0.75f, 0.75f, false, false, 0f, 0.32f, 0f, 1f, 0.35f, 1f, new Vector3(0.12f, 0f, 0.12f), new Vector3(0.01f, 0.07f, 0.02f), new Vector3(0f, 0.008f, 0f), 1.0f)),

            new(
                "wetland_growth",
                BiomeKind.Wetland,
                Weight: 1.0f,
                ProbabilityThreshold: 0.46f,
                MaxPlacementsPerChunk: 5,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 5,
                MinSlope: 0f,
                MaxSlope: 0.24f,
                MinMoisture: 0.60f,
                MaxMoisture: 1f,
                MinTemperature: 0.18f,
                MaxTemperature: 0.95f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: true,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.WetlandTree, EnvironmentLodLevel.Near, "WetlandTree", new EnvironmentAssetDescriptor("env/wetland_tree_near", EnvironmentMaterialKind.Bark), 1.8f, 1.0f, 0.65f, false, false, 0f, 0.24f, 0.18f, 0.95f, 0.60f, 1f, new Vector3(0.20f, 0f, 0.20f), new Vector3(0.02f, 0.09f, 0.02f), new Vector3(0f, 0.05f, 0f), 0.96f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.ReedPatch, EnvironmentLodLevel.Mid, "WetlandReedMid", new EnvironmentAssetDescriptor("env/wetland_reed_mid", EnvironmentMaterialKind.Reed), 1.0f, 0.85f, 1.0f, false, false, 0f, 0.24f, 0.18f, 0.95f, 0.60f, 1f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.01f, 0.07f, 0.02f), new Vector3(0f, 0.01f, 0f), 1.10f)),

            new(
                "woodland_canopy",
                BiomeKind.Woodland,
                Weight: 1.0f,
                ProbabilityThreshold: 0.56f,
                MaxPlacementsPerChunk: 4,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 6,
                MinSlope: 0f,
                MaxSlope: 0.40f,
                MinMoisture: 0.38f,
                MaxMoisture: 1f,
                MinTemperature: 0.28f,
                MaxTemperature: 0.95f,
                RequiresSoftGround: true,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.BroadleafTree, EnvironmentLodLevel.Near, "BroadleafTree", 2.2f, 1.0f, 1.0f, true, false, 0f, 0.40f, 0.28f, 0.95f, 0.38f, 1f, new Vector3(0.20f, 0f, 0.20f), new Vector3(0.02f, 0.08f, 0.02f), new Vector3(0f, 0.04f, 0f), 0.85f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Bush, EnvironmentLodLevel.Mid, "WoodlandBushMid", 1.2f, 0.85f, 1.0f, true, false, 0f, 0.40f, 0.28f, 0.95f, 0.38f, 1f, new Vector3(0.16f, 0f, 0.16f), new Vector3(0.01f, 0.05f, 0.02f), new Vector3(0f, 0.015f, 0f), 0.95f)),

            new(
                "plains_growth",
                BiomeKind.Plains,
                Weight: 1.0f,
                ProbabilityThreshold: 0.78f,
                MaxPlacementsPerChunk: 3,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 5,
                MinSlope: 0f,
                MaxSlope: 0.34f,
                MinMoisture: 0.22f,
                MaxMoisture: 0.88f,
                MinTemperature: 0.24f,
                MaxTemperature: 1f,
                RequiresSoftGround: true,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.BroadleafTree, EnvironmentLodLevel.Near, "PlainsBroadleaf", 2.0f, 0.95f, 1.0f, true, false, 0f, 0.34f, 0.24f, 1f, 0.22f, 0.88f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.02f, 0.08f, 0.02f), new Vector3(0f, 0.04f, 0f), 0.80f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Bush, EnvironmentLodLevel.Mid, "PlainsBushMid", 1.1f, 0.80f, 0.90f, true, false, 0f, 0.34f, 0.24f, 1f, 0.22f, 0.88f, new Vector3(0.14f, 0f, 0.14f), new Vector3(0.01f, 0.05f, 0.02f), new Vector3(0f, 0.015f, 0f), 1.0f)),

            new(
                "highland_growth",
                BiomeKind.Hills,
                Weight: 1.0f,
                ProbabilityThreshold: 0.66f,
                MaxPlacementsPerChunk: 4,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 5,
                MinSlope: 0.12f,
                MaxSlope: 0.48f,
                MinMoisture: 0.16f,
                MaxMoisture: 0.80f,
                MinTemperature: 0.18f,
                MaxTemperature: 0.82f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.PineTree, EnvironmentLodLevel.Near, "HighlandPine", 1.8f, 1.0f, 0.9f, false, false, 0.12f, 0.48f, 0.18f, 0.82f, 0.16f, 0.80f, new Vector3(0.18f, 0f, 0.18f), new Vector3(0.01f, 0.05f, 0.01f), new Vector3(0f, 0.03f, 0f), 0.70f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Mid, "HighlandRockMid", 1.0f, 0.80f, 0.95f, false, false, 0.12f, 0.48f, 0.18f, 0.82f, 0.16f, 0.80f, new Vector3(0.16f, 0f, 0.16f), Vector3.Zero, Vector3.Zero, 0f)),

            new(
                "scree_rocks",
                BiomeKind.Scree,
                Weight: 1.0f,
                ProbabilityThreshold: 0.42f,
                MaxPlacementsPerChunk: 5,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 4,
                MinSlope: 0.18f,
                MaxSlope: 0.82f,
                MinMoisture: 0f,
                MaxMoisture: 0.58f,
                MinTemperature: 0f,
                MaxTemperature: 0.74f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Near, "ScreeRock", 1.15f, 1.0f, 1.0f, false, false, 0.18f, 0.82f, 0f, 0.74f, 0f, 0.58f, new Vector3(0.12f, 0f, 0.12f), Vector3.Zero, Vector3.Zero, 0f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Mid, "ScreeRockMid", 0.95f, 0.70f, 0.90f, false, false, 0.18f, 0.82f, 0f, 0.74f, 0f, 0.58f, new Vector3(0.08f, 0f, 0.08f), Vector3.Zero, Vector3.Zero, 0f)),

            new(
                "alpine_sparse",
                BiomeKind.Alpine,
                Weight: 1.0f,
                ProbabilityThreshold: 0.50f,
                MaxPlacementsPerChunk: 4,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 5,
                MinSlope: 0.08f,
                MaxSlope: 0.42f,
                MinMoisture: 0.10f,
                MaxMoisture: 0.70f,
                MinTemperature: 0f,
                MaxTemperature: 0.48f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.PineTree, EnvironmentLodLevel.Near, "AlpinePine", 1.7f, 0.88f, 0.75f, false, false, 0.08f, 0.42f, 0f, 0.48f, 0.10f, 0.70f, new Vector3(0.16f, 0f, 0.16f), new Vector3(0.01f, 0.04f, 0.01f), new Vector3(0f, 0.02f, 0f), 0.62f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Mid, "AlpineRockMid", 0.95f, 0.82f, 1.0f, false, false, 0.08f, 0.42f, 0f, 0.48f, 0.10f, 0.70f, new Vector3(0.10f, 0f, 0.10f), Vector3.Zero, Vector3.Zero, 0f)),

            new(
                "mountain_rocks",
                BiomeKind.Mountains,
                Weight: 1.0f,
                ProbabilityThreshold: 0.54f,
                MaxPlacementsPerChunk: 4,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 5,
                MinSlope: 0.14f,
                MaxSlope: 0.86f,
                MinMoisture: 0f,
                MaxMoisture: 0.68f,
                MinTemperature: 0f,
                MaxTemperature: 0.62f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.RockCluster, EnvironmentLodLevel.Near, "MountainRock", 1.25f, 1.0f, 1.0f, false, false, 0.14f, 0.86f, 0f, 0.62f, 0f, 0.68f, new Vector3(0.12f, 0f, 0.12f), Vector3.Zero, Vector3.Zero, 0f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.PineTree, EnvironmentLodLevel.Mid, "MountainPineMid", 1.5f, 0.74f, 0.60f, false, false, 0.14f, 0.40f, 0f, 0.50f, 0.10f, 0.58f, new Vector3(0.14f, 0f, 0.14f), new Vector3(0.01f, 0.04f, 0.01f), new Vector3(0f, 0.02f, 0f), 0.55f)),
        };
    }

    private static List<EnvironmentSpawnRule> BuildStructureRules()
    {
        return new List<EnvironmentSpawnRule>
        {
            new(
                "coastal_ruin",
                BiomeKind.Shore,
                Weight: 1.0f,
                ProbabilityThreshold: 0.78f,
                MaxPlacementsPerChunk: 1,
                GridStep: 6,
                MinEdgePadding: 2,
                ClearanceHeight: 6,
                MinSlope: 0f,
                MaxSlope: 0.22f,
                MinMoisture: 0f,
                MaxMoisture: 1f,
                MinTemperature: 0f,
                MaxTemperature: 1f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: true,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.RuinArch, EnvironmentLodLevel.Near, "CoastalRuin", 2.1f, 1.0f, 1.0f, false, false, 0f, 0.22f, 0f, 1f, 0f, 1f, new Vector3(0.08f, 0f, 0.08f), Vector3.Zero, Vector3.Zero, 0f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Mid, "CoastalMarkerMid", 1.0f, 0.88f, 0.50f, false, false, 0f, 0.22f, 0f, 1f, 0f, 1f, new Vector3(0.06f, 0f, 0.06f), Vector3.Zero, Vector3.Zero, 0f)),

            new(
                "upland_marker",
                BiomeKind.Hills,
                Weight: 1.0f,
                ProbabilityThreshold: 0.76f,
                MaxPlacementsPerChunk: 1,
                GridStep: 6,
                MinEdgePadding: 2,
                ClearanceHeight: 6,
                MinSlope: 0f,
                MaxSlope: 0.48f,
                MinMoisture: 0f,
                MaxMoisture: 1f,
                MinTemperature: 0f,
                MaxTemperature: 1f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Near, "UplandCairn", 1.2f, 1.0f, 1.0f, false, false, 0f, 0.48f, 0f, 1f, 0f, 1f, new Vector3(0.06f, 0f, 0.06f), Vector3.Zero, Vector3.Zero, 0f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Mid, "UplandCairnMid", 0.95f, 0.70f, 0.55f, false, false, 0f, 0.48f, 0f, 1f, 0f, 1f, new Vector3(0.04f, 0f, 0.04f), Vector3.Zero, Vector3.Zero, 0f)),

            new(
                "alpine_marker",
                BiomeKind.Alpine,
                Weight: 1.0f,
                ProbabilityThreshold: 0.72f,
                MaxPlacementsPerChunk: 1,
                GridStep: 6,
                MinEdgePadding: 2,
                ClearanceHeight: 6,
                MinSlope: 0f,
                MaxSlope: 0.52f,
                MinMoisture: 0f,
                MaxMoisture: 1f,
                MinTemperature: 0f,
                MaxTemperature: 1f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Near, "AlpineCairn", 1.2f, 1.0f, 1.0f, false, false, 0f, 0.52f, 0f, 1f, 0f, 1f, new Vector3(0.05f, 0f, 0.05f), Vector3.Zero, Vector3.Zero, 0f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Cairn, EnvironmentLodLevel.Mid, "AlpineCairnMid", 0.90f, 0.68f, 0.55f, false, false, 0f, 0.52f, 0f, 1f, 0f, 1f, new Vector3(0.04f, 0f, 0.04f), Vector3.Zero, Vector3.Zero, 0f)),
        };
    }

    private static List<EnvironmentSpawnRule> BuildAnimalRules()
    {
        return new List<EnvironmentSpawnRule>
        {
            new(
                "shore_gulls",
                BiomeKind.Shore,
                Weight: 1.0f,
                ProbabilityThreshold: 0.70f,
                MaxPlacementsPerChunk: 1,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 4,
                MinSlope: 0f,
                MaxSlope: 0.35f,
                MinMoisture: 0f,
                MaxMoisture: 1f,
                MinTemperature: 0f,
                MaxTemperature: 1f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: true,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.Gull, EnvironmentLodLevel.Near, "ShoreGull", 1.1f, 1.0f, 1.0f, false, true, 0f, 0.35f, 0f, 1f, 0f, 1f, new Vector3(0.10f, 0f, 0.10f), new Vector3(0.05f, 0.18f, 0.05f), new Vector3(0f, 0.12f, 0f), 1.55f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Gull, EnvironmentLodLevel.Mid, "ShoreGullMid", 0.72f, 0.70f, 0.60f, false, true, 0f, 0.35f, 0f, 1f, 0f, 1f, new Vector3(0.08f, 0f, 0.08f), new Vector3(0.03f, 0.12f, 0.03f), new Vector3(0f, 0.08f, 0f), 1.1f)),

            new(
                "upland_deer",
                BiomeKind.Plains,
                Weight: 1.0f,
                ProbabilityThreshold: 0.72f,
                MaxPlacementsPerChunk: 1,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 4,
                MinSlope: 0f,
                MaxSlope: 0.35f,
                MinMoisture: 0.10f,
                MaxMoisture: 0.90f,
                MinTemperature: 0.20f,
                MaxTemperature: 1f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.Deer, EnvironmentLodLevel.Near, "PlainsDeer", 1.4f, 1.0f, 1.0f, false, false, 0f, 0.35f, 0.20f, 1f, 0.10f, 0.90f, new Vector3(0.10f, 0f, 0.10f), new Vector3(0.02f, 0.10f, 0.01f), new Vector3(0f, 0.03f, 0f), 0.92f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Deer, EnvironmentLodLevel.Mid, "PlainsDeerMid", 1.0f, 0.72f, 0.55f, false, false, 0f, 0.35f, 0.20f, 1f, 0.10f, 0.90f, new Vector3(0.08f, 0f, 0.08f), new Vector3(0.01f, 0.06f, 0.01f), new Vector3(0f, 0.02f, 0f), 0.70f)),

            new(
                "mountain_goats",
                BiomeKind.Mountains,
                Weight: 1.0f,
                ProbabilityThreshold: 0.68f,
                MaxPlacementsPerChunk: 1,
                GridStep: 4,
                MinEdgePadding: 2,
                ClearanceHeight: 4,
                MinSlope: 0f,
                MaxSlope: 0.40f,
                MinMoisture: 0f,
                MaxMoisture: 0.80f,
                MinTemperature: 0f,
                MaxTemperature: 0.70f,
                RequiresSoftGround: false,
                AllowsWaterAdjacency: false,
                NearVariant: new EnvironmentPropVariant(EnvironmentPropKind.Goat, EnvironmentLodLevel.Near, "MountainGoat", 1.2f, 1.0f, 1.0f, false, false, 0f, 0.40f, 0f, 0.70f, 0f, 0.80f, new Vector3(0.10f, 0f, 0.10f), new Vector3(0.03f, 0.12f, 0.01f), new Vector3(0f, 0.02f, 0f), 1.05f),
                MidVariant: new EnvironmentPropVariant(EnvironmentPropKind.Goat, EnvironmentLodLevel.Mid, "MountainGoatMid", 0.86f, 0.72f, 0.58f, false, false, 0f, 0.40f, 0f, 0.70f, 0f, 0.80f, new Vector3(0.08f, 0f, 0.08f), new Vector3(0.02f, 0.07f, 0.01f), new Vector3(0f, 0.015f, 0f), 0.72f)),
        };
    }
}
