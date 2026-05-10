#nullable enable
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class TerrainEnvironmentDecorator
{
    private readonly TerrainGenerationSettings settings;
    private readonly TerrainChunkGenerator generator;
    private readonly EnvironmentVisualFactory visualFactory;
    private readonly TerrainEnvironmentRuleSet ruleSet = new();

    public TerrainEnvironmentDecorator(
        TerrainGenerationSettings settings,
        TerrainChunkGenerator generator,
        EnvironmentVisualFactory visualFactory)
    {
        this.settings = settings;
        this.generator = generator;
        this.visualFactory = visualFactory;
    }

    public void DecorateChunk(Entity chunkEntity, VoxelChunkData chunkData, Vector3? focusWorldPosition = null)
    {
        var environmentRoot = new Entity("EnvironmentDecor");
        Vector3 focus = focusWorldPosition ?? GetChunkCenterWorld(chunkData);
        int placedCount = 0;

        placedCount += PlaceRuleGroup(environmentRoot, chunkData, focus, ruleSet.VegetationRules);
        placedCount += PlaceRuleGroup(environmentRoot, chunkData, focus, ruleSet.StructureRules);
        placedCount += PlaceRuleGroup(environmentRoot, chunkData, focus, ruleSet.AnimalRules);

        if (placedCount > 0)
        {
            chunkEntity.AddChild(environmentRoot);
        }
    }

    public int CountEntitiesForChunk(VoxelChunkData chunkData, Vector3? focusWorldPosition = null)
    {
        Vector3 focus = focusWorldPosition ?? GetChunkCenterWorld(chunkData);
        int placedCount = 0;
        placedCount += CountRuleGroup(chunkData, focus, ruleSet.VegetationRules);
        placedCount += CountRuleGroup(chunkData, focus, ruleSet.StructureRules);
        placedCount += CountRuleGroup(chunkData, focus, ruleSet.AnimalRules);
        return placedCount;
    }

    private int PlaceRuleGroup(Entity root, VoxelChunkData chunkData, Vector3 focusWorldPosition, IReadOnlyList<EnvironmentSpawnRule> rules)
    {
        int placed = 0;
        foreach (EnvironmentSpawnRule rule in rules)
        {
            placed += PlaceRule(root, chunkData, focusWorldPosition, rule);
        }

        return placed;
    }

    private int CountRuleGroup(VoxelChunkData chunkData, Vector3 focusWorldPosition, IReadOnlyList<EnvironmentSpawnRule> rules)
    {
        int placed = 0;
        foreach (EnvironmentSpawnRule rule in rules)
        {
            placed += CountRule(chunkData, focusWorldPosition, rule);
        }

        return placed;
    }

    private int PlaceRule(Entity root, VoxelChunkData chunkData, Vector3 focusWorldPosition, EnvironmentSpawnRule rule)
    {
        int placed = 0;
        for (int localZ = rule.MinEdgePadding; localZ < chunkData.Size - rule.MinEdgePadding && placed < rule.MaxPlacementsPerChunk; localZ += Math.Max(1, rule.GridStep))
        {
            for (int localX = rule.MinEdgePadding; localX < chunkData.Size - rule.MinEdgePadding && placed < rule.MaxPlacementsPerChunk; localX += Math.Max(1, rule.GridStep))
            {
                if (!TryGetPlacementContext(chunkData, localX, localZ, rule.ClearanceHeight, out PlacementContext context))
                {
                    continue;
                }

                if (!MatchesRule(context, rule))
                {
                    continue;
                }

                float random = Hash01(context.WorldX, context.WorldZ, rule.Name.GetHashCode());
                if (random < rule.ProbabilityThreshold)
                {
                    continue;
                }

                EnvironmentPropVariant variant = SelectVariant(rule, context, focusWorldPosition);
                EnvironmentPlacementRecord placement = CreatePlacement(context, variant, random);
                Entity entity = CreatePropEntity(placement);
                root.AddChild(entity);
                placed++;
            }
        }

        return placed;
    }

    private int CountRule(VoxelChunkData chunkData, Vector3 focusWorldPosition, EnvironmentSpawnRule rule)
    {
        int placed = 0;
        for (int localZ = rule.MinEdgePadding; localZ < chunkData.Size - rule.MinEdgePadding && placed < rule.MaxPlacementsPerChunk; localZ += Math.Max(1, rule.GridStep))
        {
            for (int localX = rule.MinEdgePadding; localX < chunkData.Size - rule.MinEdgePadding && placed < rule.MaxPlacementsPerChunk; localX += Math.Max(1, rule.GridStep))
            {
                if (!TryGetPlacementContext(chunkData, localX, localZ, rule.ClearanceHeight, out PlacementContext context))
                {
                    continue;
                }

                if (!MatchesRule(context, rule))
                {
                    continue;
                }

                float random = Hash01(context.WorldX, context.WorldZ, rule.Name.GetHashCode());
                if (random < rule.ProbabilityThreshold)
                {
                    continue;
                }

                SelectVariant(rule, context, focusWorldPosition);
                placed++;
            }
        }

        return placed;
    }

    private bool MatchesRule(PlacementContext context, EnvironmentSpawnRule rule)
    {
        if (context.Sample.Biome != rule.PrimaryBiome)
        {
            return false;
        }

        if (context.Sample.Slope < rule.MinSlope || context.Sample.Slope > rule.MaxSlope)
        {
            return false;
        }

        if (context.Sample.Moisture < rule.MinMoisture || context.Sample.Moisture > rule.MaxMoisture)
        {
            return false;
        }

        if (context.Sample.Temperature < rule.MinTemperature || context.Sample.Temperature > rule.MaxTemperature)
        {
            return false;
        }

        if (rule.RequiresSoftGround && !context.IsSoftGround)
        {
            return false;
        }

        if (!rule.AllowsWaterAdjacency && context.SurfaceY <= context.Sample.WaterLevel + 1)
        {
            return false;
        }

        return true;
    }

    private EnvironmentPropVariant SelectVariant(EnvironmentSpawnRule rule, PlacementContext context, Vector3 focusWorldPosition)
    {
        float distance = Vector3.Distance(
            new Vector3(context.WorldX + 0.5f, context.BasePosition.Y, context.WorldZ + 0.5f),
            focusWorldPosition);

        if (distance <= 22f)
        {
            return rule.NearVariant;
        }

        if (distance <= 42f)
        {
            return rule.MidVariant;
        }

        return rule.FarVariant ?? rule.MidVariant;
    }

    private static EnvironmentPlacementRecord CreatePlacement(PlacementContext context, EnvironmentPropVariant variant, float random)
    {
        float jitterX = (Hash01(context.WorldX, context.WorldZ, 877) * 2f - 1f) * variant.PositionJitter.X;
        float jitterZ = (Hash01(context.WorldX, context.WorldZ, 911) * 2f - 1f) * variant.PositionJitter.Z;
        float yaw = random * MathF.PI * 2f;

        Vector3 localPosition = context.BasePosition + new Vector3(jitterX, 0f, jitterZ);
        return new EnvironmentPlacementRecord(
            variant,
            localPosition,
            yaw,
            random,
            context.Sample);
    }

    private Entity CreatePropEntity(EnvironmentPlacementRecord placement)
    {
        return placement.Variant.Kind switch
        {
            EnvironmentPropKind.BroadleafTree => CreateBroadleafTree(placement),
            EnvironmentPropKind.PineTree => CreatePineTree(placement),
            EnvironmentPropKind.WetlandTree => CreateWetlandTree(placement),
            EnvironmentPropKind.Bush => CreateBush(placement),
            EnvironmentPropKind.ReedPatch => CreateReedPatch(placement),
            EnvironmentPropKind.RockCluster => CreateRockCluster(placement),
            EnvironmentPropKind.Cairn => CreateCairn(placement),
            EnvironmentPropKind.RuinArch => CreateRuinArch(placement),
            EnvironmentPropKind.Deer => CreateDeer(placement),
            EnvironmentPropKind.Goat => CreateGoat(placement),
            EnvironmentPropKind.Gull => CreateGull(placement),
            _ => CreateRockCluster(placement),
        };
    }

    private bool TryGetPlacementContext(VoxelChunkData chunkData, int localX, int localZ, int clearanceHeight, out PlacementContext context)
    {
        int surfaceY = chunkData.GetSurfaceHeight(localX, localZ);
        if (surfaceY <= 0 || surfaceY >= chunkData.Height - clearanceHeight - 1)
        {
            context = default;
            return false;
        }

        BlockKind groundBlock = chunkData.GetBlock(localX, surfaceY, localZ);
        if (groundBlock is BlockKind.Air or BlockKind.Water)
        {
            context = default;
            return false;
        }

        for (int y = 1; y <= clearanceHeight; y++)
        {
            if (chunkData.GetBlock(localX, surfaceY + y, localZ) is not BlockKind.Air and not BlockKind.Water)
            {
                context = default;
                return false;
            }
        }

        int worldX = chunkData.Coordinate.X * chunkData.Size + localX;
        int worldZ = chunkData.Coordinate.Z * chunkData.Size + localZ;
        WorldSample sample = generator.SampleSurfaceWorld(worldX, worldZ);
        Vector3 basePosition = new(
            (localX + 0.5f) * settings.VoxelScale,
            (surfaceY + 1f) * settings.VoxelScale,
            (localZ + 0.5f) * settings.VoxelScale);

        context = new PlacementContext(
            chunkData,
            sample,
            worldX,
            worldZ,
            surfaceY,
            groundBlock,
            basePosition,
            groundBlock is BlockKind.Grass or BlockKind.Dirt or BlockKind.Mud or BlockKind.Peat or BlockKind.Moss or BlockKind.Snow);
        return true;
    }

    private Entity CreateBroadleafTree(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateEnvironmentEntity("Trunk", new EnvironmentAssetDescriptor($"{placement.Variant.Asset.AssetKey}/trunk", EnvironmentMaterialKind.Bark), new Vector3(0f, 1.25f * scale, 0f), new Vector3(0.42f, 2.5f * scale, 0.42f)));
        root.AddChild(visualFactory.CreateEnvironmentEntity("CanopyA", new EnvironmentAssetDescriptor($"{placement.Variant.Asset.AssetKey}/canopy_a", EnvironmentMaterialKind.Leaf), new Vector3(0f, 3.25f * scale, 0f), new Vector3(1.85f * scale, 1.45f * scale, 1.85f * scale)));
        root.AddChild(visualFactory.CreateEnvironmentEntity("CanopyB", new EnvironmentAssetDescriptor($"{placement.Variant.Asset.AssetKey}/canopy_b", EnvironmentMaterialKind.Leaf), new Vector3(0.35f * scale, 4.15f * scale, -0.15f * scale), new Vector3(1.15f * scale, 0.95f * scale, 1.15f * scale)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private Entity CreatePineTree(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateEnvironmentEntity("Trunk", new EnvironmentAssetDescriptor($"{placement.Variant.Asset.AssetKey}/trunk", EnvironmentMaterialKind.Bark), new Vector3(0f, 1.5f * scale, 0f), new Vector3(0.35f, 3.0f * scale, 0.35f)));
        root.AddChild(visualFactory.CreateEnvironmentEntity("NeedlesA", new EnvironmentAssetDescriptor($"{placement.Variant.Asset.AssetKey}/needles_a", EnvironmentMaterialKind.Needle), new Vector3(0f, 2.25f * scale, 0f), new Vector3(1.30f * scale, 1.00f * scale, 1.30f * scale)));
        root.AddChild(visualFactory.CreateEnvironmentEntity("NeedlesB", new EnvironmentAssetDescriptor($"{placement.Variant.Asset.AssetKey}/needles_b", EnvironmentMaterialKind.Needle), new Vector3(0f, 3.35f * scale, 0f), new Vector3(1.00f * scale, 0.95f * scale, 1.00f * scale)));
        root.AddChild(visualFactory.CreateEnvironmentEntity("NeedlesC", new EnvironmentAssetDescriptor($"{placement.Variant.Asset.AssetKey}/needles_c", EnvironmentMaterialKind.Needle), new Vector3(0f, 4.20f * scale, 0f), new Vector3(0.66f * scale, 0.78f * scale, 0.66f * scale)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private Entity CreateWetlandTree(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("Trunk", EnvironmentMaterialKind.Bark, new Vector3(0f, 1.05f * scale, 0f), new Vector3(0.30f, 2.10f * scale, 0.30f)));
        root.AddChild(visualFactory.CreateBoxEntity("CanopyA", EnvironmentMaterialKind.Leaf, new Vector3(0f, 2.75f * scale, 0f), new Vector3(1.40f * scale, 1.10f * scale, 1.40f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("CanopyB", EnvironmentMaterialKind.Leaf, new Vector3(0.28f * scale, 3.35f * scale, 0.12f * scale), new Vector3(0.88f * scale, 0.72f * scale, 0.88f * scale)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private Entity CreateBush(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("BushA", EnvironmentMaterialKind.Leaf, new Vector3(0f, 0.55f * scale, 0f), new Vector3(1.05f * scale, 0.70f * scale, 1.05f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("BushB", EnvironmentMaterialKind.Leaf, new Vector3(0.35f * scale, 0.78f * scale, -0.15f * scale), new Vector3(0.70f * scale, 0.48f * scale, 0.70f * scale)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private Entity CreateReedPatch(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("ReedA", EnvironmentMaterialKind.Reed, new Vector3(-0.20f * scale, 0.65f * scale, 0.05f * scale), new Vector3(0.08f, 1.30f * scale, 0.08f), new Vector3(0f, 0f, -0.08f)));
        root.AddChild(visualFactory.CreateBoxEntity("ReedB", EnvironmentMaterialKind.Reed, new Vector3(0.15f * scale, 0.52f * scale, -0.15f * scale), new Vector3(0.08f, 1.05f * scale, 0.08f), new Vector3(0.06f, 0f, 0.08f)));
        root.AddChild(visualFactory.CreateBoxEntity("ReedC", EnvironmentMaterialKind.Reed, new Vector3(0.05f * scale, 0.75f * scale, 0.20f * scale), new Vector3(0.08f, 1.50f * scale, 0.08f), new Vector3(-0.04f, 0f, -0.05f)));
        root.AddChild(visualFactory.CreateBoxEntity("ReedD", EnvironmentMaterialKind.Reed, new Vector3(-0.10f * scale, 0.42f * scale, -0.20f * scale), new Vector3(0.08f, 0.84f * scale, 0.08f), new Vector3(0.04f, 0f, 0.03f)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private Entity CreateRockCluster(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("RockA", EnvironmentMaterialKind.Stone, new Vector3(-0.25f * scale, 0.28f * scale, 0.12f * scale), new Vector3(0.75f * scale, 0.56f * scale, 0.55f * scale), new Vector3(0.10f, 0.15f, 0.04f)));
        root.AddChild(visualFactory.CreateBoxEntity("RockB", EnvironmentMaterialKind.Stone, new Vector3(0.18f * scale, 0.20f * scale, -0.12f * scale), new Vector3(0.56f * scale, 0.40f * scale, 0.46f * scale), new Vector3(0.03f, -0.08f, 0.10f)));
        root.AddChild(visualFactory.CreateBoxEntity("RockC", EnvironmentMaterialKind.Stone, new Vector3(0.02f * scale, 0.42f * scale, 0.25f * scale), new Vector3(0.40f * scale, 0.30f * scale, 0.32f * scale), new Vector3(0.12f, 0.02f, -0.07f)));
        return root;
    }

    private Entity CreateCairn(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("Base", EnvironmentMaterialKind.RuinStone, new Vector3(0f, 0.24f * scale, 0f), new Vector3(1.10f * scale, 0.48f * scale, 1.10f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("Mid", EnvironmentMaterialKind.Stone, new Vector3(0.08f * scale, 0.74f * scale, -0.05f * scale), new Vector3(0.74f * scale, 0.36f * scale, 0.74f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("Top", EnvironmentMaterialKind.Stone, new Vector3(-0.04f * scale, 1.15f * scale, 0.08f * scale), new Vector3(0.44f * scale, 0.26f * scale, 0.44f * scale)));
        return root;
    }

    private Entity CreateRuinArch(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition, placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("PillarA", EnvironmentMaterialKind.RuinStone, new Vector3(-0.70f * scale, 1.25f * scale, 0f), new Vector3(0.42f * scale, 2.50f * scale, 0.42f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("PillarB", EnvironmentMaterialKind.RuinStone, new Vector3(0.70f * scale, 1.12f * scale, 0f), new Vector3(0.42f * scale, 2.24f * scale, 0.42f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("Lintel", EnvironmentMaterialKind.RuinStone, new Vector3(0f, 2.42f * scale, 0f), new Vector3(1.72f * scale, 0.34f * scale, 0.52f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("BrokenStone", EnvironmentMaterialKind.Stone, new Vector3(1.22f * scale, 0.34f * scale, -0.18f * scale), new Vector3(0.42f * scale, 0.32f * scale, 0.46f * scale), new Vector3(0.14f, 0.20f, 0.10f)));
        return root;
    }

    private Entity CreateDeer(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition + new Vector3(0f, 0.18f * scale, 0f), placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("Body", EnvironmentMaterialKind.Deer, new Vector3(0f, 0.82f * scale, 0f), new Vector3(1.20f * scale, 0.65f * scale, 0.48f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("Neck", EnvironmentMaterialKind.Deer, new Vector3(0.48f * scale, 1.20f * scale, 0f), new Vector3(0.24f * scale, 0.56f * scale, 0.22f * scale), new Vector3(-0.08f, 0f, 0.18f)));
        root.AddChild(visualFactory.CreateBoxEntity("Head", EnvironmentMaterialKind.Deer, new Vector3(0.70f * scale, 1.44f * scale, 0f), new Vector3(0.46f * scale, 0.28f * scale, 0.24f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegA", EnvironmentMaterialKind.Deer, new Vector3(-0.34f * scale, 0.28f * scale, 0.14f * scale), new Vector3(0.12f * scale, 0.56f * scale, 0.12f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegB", EnvironmentMaterialKind.Deer, new Vector3(0.22f * scale, 0.28f * scale, 0.14f * scale), new Vector3(0.12f * scale, 0.56f * scale, 0.12f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegC", EnvironmentMaterialKind.Deer, new Vector3(-0.34f * scale, 0.28f * scale, -0.14f * scale), new Vector3(0.12f * scale, 0.56f * scale, 0.12f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegD", EnvironmentMaterialKind.Deer, new Vector3(0.22f * scale, 0.28f * scale, -0.14f * scale), new Vector3(0.12f * scale, 0.56f * scale, 0.12f * scale)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private Entity CreateGoat(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition + new Vector3(0f, 0.16f * scale, 0f), placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("Body", EnvironmentMaterialKind.Goat, new Vector3(0f, 0.72f * scale, 0f), new Vector3(0.95f * scale, 0.56f * scale, 0.42f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("Head", EnvironmentMaterialKind.Goat, new Vector3(0.52f * scale, 1.08f * scale, 0f), new Vector3(0.34f * scale, 0.26f * scale, 0.22f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegA", EnvironmentMaterialKind.Goat, new Vector3(-0.25f * scale, 0.24f * scale, 0.12f * scale), new Vector3(0.10f * scale, 0.48f * scale, 0.10f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegB", EnvironmentMaterialKind.Goat, new Vector3(0.20f * scale, 0.24f * scale, 0.12f * scale), new Vector3(0.10f * scale, 0.48f * scale, 0.10f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegC", EnvironmentMaterialKind.Goat, new Vector3(-0.25f * scale, 0.24f * scale, -0.12f * scale), new Vector3(0.10f * scale, 0.48f * scale, 0.10f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("LegD", EnvironmentMaterialKind.Goat, new Vector3(0.20f * scale, 0.24f * scale, -0.12f * scale), new Vector3(0.10f * scale, 0.48f * scale, 0.10f * scale)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private Entity CreateGull(EnvironmentPlacementRecord placement)
    {
        float scale = placement.Variant.HeightScale;
        var root = CreateRootEntity(placement.Variant.Name, placement.LocalPosition + new Vector3(0f, 0.95f * scale, 0f), placement.YawRadians);
        root.AddChild(visualFactory.CreateBoxEntity("Body", EnvironmentMaterialKind.Gull, new Vector3(0f, 0.20f * scale, 0f), new Vector3(0.42f * scale, 0.20f * scale, 0.22f * scale)));
        root.AddChild(visualFactory.CreateBoxEntity("WingL", EnvironmentMaterialKind.Gull, new Vector3(0f, 0.18f * scale, -0.22f * scale), new Vector3(0.82f * scale, 0.06f * scale, 0.12f * scale), new Vector3(0f, 0f, -0.12f)));
        root.AddChild(visualFactory.CreateBoxEntity("WingR", EnvironmentMaterialKind.Gull, new Vector3(0f, 0.18f * scale, 0.22f * scale), new Vector3(0.82f * scale, 0.06f * scale, 0.12f * scale), new Vector3(0f, 0f, 0.12f)));
        AttachMotion(root, placement.Variant.PositionAmplitude, placement.Variant.RotationAmplitude, placement.Variant.MotionSpeed, placement.Variation);
        return root;
    }

    private static Entity CreateRootEntity(string name, Vector3 position, float yawRadians)
    {
        var entity = new Entity(name);
        entity.Transform.Position = position;
        entity.Transform.RotationEulerXYZ = new Vector3(0f, yawRadians, 0f);
        return entity;
    }

    private static void AttachMotion(Entity entity, Vector3 positionAmplitude, Vector3 rotationAmplitudeEuler, float speed, float phase)
    {
        if (speed <= 0f || (positionAmplitude == Vector3.Zero && rotationAmplitudeEuler == Vector3.Zero))
        {
            return;
        }

        entity.Add(new AmbientMotionScript
        {
            PositionAmplitude = positionAmplitude,
            RotationAmplitudeEuler = rotationAmplitudeEuler,
            Speed = speed,
            Phase = phase * MathF.PI * 2f,
        });
    }

    private Vector3 GetChunkCenterWorld(VoxelChunkData chunkData)
    {
        float baseX = (chunkData.Coordinate.X * chunkData.Size + chunkData.Size * 0.5f) * settings.VoxelScale;
        float baseZ = (chunkData.Coordinate.Z * chunkData.Size + chunkData.Size * 0.5f) * settings.VoxelScale;
        return new Vector3(baseX, 0f, baseZ);
    }

    private static float Hash01(int x, int z, int seed)
    {
        int hash = x * 374761393 + z * 668265263 + seed * 1442695041;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash ^= hash >> 16;
        return (hash & 1023) / 1023f;
    }

    private readonly record struct PlacementContext(
        VoxelChunkData Chunk,
        WorldSample Sample,
        int WorldX,
        int WorldZ,
        int SurfaceY,
        BlockKind GroundBlock,
        Vector3 BasePosition,
        bool IsSoftGround);
}
