#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class TerrainEnvironmentDecorator
{
    private readonly TerrainGenerationSettings settings;
    private readonly TerrainChunkGenerator generator;
    private readonly EnvironmentVisualFactory visualFactory;

    public TerrainEnvironmentDecorator(
        TerrainGenerationSettings settings,
        TerrainChunkGenerator generator,
        EnvironmentVisualFactory visualFactory)
    {
        this.settings = settings;
        this.generator = generator;
        this.visualFactory = visualFactory;
    }

    public void DecorateChunk(Entity chunkEntity, VoxelChunkData chunkData)
    {
        var environmentRoot = new Entity("EnvironmentDecor");
        int placedCount = 0;

        placedCount += AddVegetation(environmentRoot, chunkData);
        placedCount += AddStructure(environmentRoot, chunkData);
        placedCount += AddAmbientAnimal(environmentRoot, chunkData);

        if (placedCount > 0)
        {
            chunkEntity.AddChild(environmentRoot);
        }
    }

    private int AddVegetation(Entity root, VoxelChunkData chunkData)
    {
        int placed = 0;
        for (int localZ = 2; localZ < chunkData.Size - 2 && placed < 4; localZ += 4)
        {
            for (int localX = 2; localX < chunkData.Size - 2 && placed < 4; localX += 4)
            {
                if (!TryGetPlacementContext(chunkData, localX, localZ, 5, out PlacementContext context))
                {
                    continue;
                }

                float random = Hash01(context.WorldX, context.WorldZ, 101);
                Entity? prop = context.Sample.Biome switch
                {
                    BiomeKind.Shore when context.SurfaceY <= context.Sample.WaterLevel + 3 && random > 0.55f
                        => CreateReedPatch(context, random),
                    BiomeKind.Plains when context.Sample.Slope < 0.34f && context.IsSoftGround && random > 0.78f
                        => random > 0.90f ? CreateBroadleafTree(context, random) : CreateBush(context, random),
                    BiomeKind.Hills when context.Sample.Slope < 0.48f && random > 0.66f
                        => random > 0.85f ? CreatePineTree(context, random) : CreateRockCluster(context, random),
                    BiomeKind.Mountains when random > 0.56f
                        => context.Sample.Temperature < 0.45f && context.Sample.Slope < 0.40f && random > 0.86f
                            ? CreatePineTree(context, random)
                            : CreateRockCluster(context, random),
                    _ => null,
                };

                if (prop is null)
                {
                    continue;
                }

                root.AddChild(prop);
                placed++;
            }
        }

        return placed;
    }

    private int AddStructure(Entity root, VoxelChunkData chunkData)
    {
        if (Hash01(chunkData.Coordinate.X, chunkData.Coordinate.Z, 211) < 0.76f)
        {
            return 0;
        }

        for (int attempt = 0; attempt < 2; attempt++)
        {
            int localX = 2 + HashInt(chunkData.Coordinate.X, chunkData.Coordinate.Z, 307 + attempt * 13, chunkData.Size - 4);
            int localZ = 2 + HashInt(chunkData.Coordinate.Z, chunkData.Coordinate.X, 401 + attempt * 19, chunkData.Size - 4);

            if (!TryGetPlacementContext(chunkData, localX, localZ, 6, out PlacementContext context))
            {
                continue;
            }

            if (context.Sample.Biome == BiomeKind.Shore && context.Sample.Slope > 0.22f)
            {
                continue;
            }

            if (context.Sample.Biome != BiomeKind.Shore && context.Sample.Slope > 0.48f)
            {
                continue;
            }

            Entity structure = context.Sample.Biome is BiomeKind.Hills or BiomeKind.Mountains
                ? CreateCairn(context, Hash01(context.WorldX, context.WorldZ, 419))
                : CreateRuinArch(context, Hash01(context.WorldX, context.WorldZ, 433));
            root.AddChild(structure);
            return 1;
        }

        return 0;
    }

    private int AddAmbientAnimal(Entity root, VoxelChunkData chunkData)
    {
        if (Hash01(chunkData.Coordinate.X, chunkData.Coordinate.Z, 509) < 0.72f)
        {
            return 0;
        }

        int localX = 2 + HashInt(chunkData.Coordinate.X, chunkData.Coordinate.Z, 547, chunkData.Size - 4);
        int localZ = 2 + HashInt(chunkData.Coordinate.Z, chunkData.Coordinate.X, 563, chunkData.Size - 4);
        if (!TryGetPlacementContext(chunkData, localX, localZ, 4, out PlacementContext context))
        {
            return 0;
        }

        if (context.Sample.Biome != BiomeKind.Mountains && context.Sample.Slope > 0.35f)
        {
            return 0;
        }

        Entity? animal = context.Sample.Biome switch
        {
            BiomeKind.Shore => CreateGull(context, Hash01(context.WorldX, context.WorldZ, 571)),
            BiomeKind.Mountains => CreateGoat(context, Hash01(context.WorldX, context.WorldZ, 587)),
            _ => CreateDeer(context, Hash01(context.WorldX, context.WorldZ, 593)),
        };

        root.AddChild(animal);
        return 1;
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
            groundBlock is BlockKind.Grass or BlockKind.Dirt);
        return true;
    }

    private Entity CreateBroadleafTree(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("BroadleafTree", context.BasePosition, variation);
        root.AddChild(visualFactory.CreateBoxEntity("Trunk", EnvironmentMaterialKind.Bark, new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 2.5f, 0.42f)));
        root.AddChild(visualFactory.CreateBoxEntity("CanopyA", EnvironmentMaterialKind.Leaf, new Vector3(0f, 3.25f, 0f), new Vector3(1.85f, 1.45f, 1.85f)));
        root.AddChild(visualFactory.CreateBoxEntity("CanopyB", EnvironmentMaterialKind.Leaf, new Vector3(0.35f, 4.15f, -0.15f), new Vector3(1.15f, 0.95f, 1.15f)));
        AttachMotion(root, new Vector3(0f, 0.04f, 0f), new Vector3(0.02f, 0.08f, 0.02f), 0.85f + variation * 0.45f, variation);
        return root;
    }

    private Entity CreatePineTree(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("PineTree", context.BasePosition, variation);
        root.AddChild(visualFactory.CreateBoxEntity("Trunk", EnvironmentMaterialKind.Bark, new Vector3(0f, 1.5f, 0f), new Vector3(0.35f, 3.0f, 0.35f)));
        root.AddChild(visualFactory.CreateBoxEntity("NeedlesA", EnvironmentMaterialKind.Needle, new Vector3(0f, 2.25f, 0f), new Vector3(1.30f, 1.00f, 1.30f)));
        root.AddChild(visualFactory.CreateBoxEntity("NeedlesB", EnvironmentMaterialKind.Needle, new Vector3(0f, 3.35f, 0f), new Vector3(1.00f, 0.95f, 1.00f)));
        root.AddChild(visualFactory.CreateBoxEntity("NeedlesC", EnvironmentMaterialKind.Needle, new Vector3(0f, 4.20f, 0f), new Vector3(0.66f, 0.78f, 0.66f)));
        AttachMotion(root, new Vector3(0f, 0.03f, 0f), new Vector3(0.01f, 0.05f, 0.01f), 0.70f + variation * 0.35f, variation);
        return root;
    }

    private Entity CreateBush(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("Bush", context.BasePosition, variation);
        root.AddChild(visualFactory.CreateBoxEntity("BushA", EnvironmentMaterialKind.Leaf, new Vector3(0f, 0.55f, 0f), new Vector3(1.05f, 0.70f, 1.05f)));
        root.AddChild(visualFactory.CreateBoxEntity("BushB", EnvironmentMaterialKind.Leaf, new Vector3(0.35f, 0.78f, -0.15f), new Vector3(0.70f, 0.48f, 0.70f)));
        AttachMotion(root, new Vector3(0f, 0.02f, 0f), new Vector3(0.01f, 0.05f, 0.02f), 1.1f + variation * 0.35f, variation);
        return root;
    }

    private Entity CreateReedPatch(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("ReedPatch", context.BasePosition, variation);
        root.AddChild(visualFactory.CreateBoxEntity("ReedA", EnvironmentMaterialKind.Reed, new Vector3(-0.20f, 0.65f, 0.05f), new Vector3(0.08f, 1.30f, 0.08f), new Vector3(0f, 0f, -0.08f)));
        root.AddChild(visualFactory.CreateBoxEntity("ReedB", EnvironmentMaterialKind.Reed, new Vector3(0.15f, 0.52f, -0.15f), new Vector3(0.08f, 1.05f, 0.08f), new Vector3(0.06f, 0f, 0.08f)));
        root.AddChild(visualFactory.CreateBoxEntity("ReedC", EnvironmentMaterialKind.Reed, new Vector3(0.05f, 0.75f, 0.20f), new Vector3(0.08f, 1.50f, 0.08f), new Vector3(-0.04f, 0f, -0.05f)));
        root.AddChild(visualFactory.CreateBoxEntity("ReedD", EnvironmentMaterialKind.Reed, new Vector3(-0.10f, 0.42f, -0.20f), new Vector3(0.08f, 0.84f, 0.08f), new Vector3(0.04f, 0f, 0.03f)));
        AttachMotion(root, new Vector3(0f, 0.01f, 0f), new Vector3(0.02f, 0.11f, 0.03f), 1.35f + variation * 0.60f, variation);
        return root;
    }

    private Entity CreateRockCluster(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("RockCluster", context.BasePosition, variation);
        root.AddChild(visualFactory.CreateBoxEntity("RockA", EnvironmentMaterialKind.Stone, new Vector3(-0.25f, 0.28f, 0.12f), new Vector3(0.75f, 0.56f, 0.55f), new Vector3(0.10f, 0.15f, 0.04f)));
        root.AddChild(visualFactory.CreateBoxEntity("RockB", EnvironmentMaterialKind.Stone, new Vector3(0.18f, 0.20f, -0.12f), new Vector3(0.56f, 0.40f, 0.46f), new Vector3(0.03f, -0.08f, 0.10f)));
        root.AddChild(visualFactory.CreateBoxEntity("RockC", EnvironmentMaterialKind.Stone, new Vector3(0.02f, 0.42f, 0.25f), new Vector3(0.40f, 0.30f, 0.32f), new Vector3(0.12f, 0.02f, -0.07f)));
        return root;
    }

    private Entity CreateCairn(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("Cairn", context.BasePosition, variation);
        root.AddChild(visualFactory.CreateBoxEntity("Base", EnvironmentMaterialKind.RuinStone, new Vector3(0f, 0.24f, 0f), new Vector3(1.10f, 0.48f, 1.10f)));
        root.AddChild(visualFactory.CreateBoxEntity("Mid", EnvironmentMaterialKind.Stone, new Vector3(0.08f, 0.74f, -0.05f), new Vector3(0.74f, 0.36f, 0.74f)));
        root.AddChild(visualFactory.CreateBoxEntity("Top", EnvironmentMaterialKind.Stone, new Vector3(-0.04f, 1.15f, 0.08f), new Vector3(0.44f, 0.26f, 0.44f)));
        return root;
    }

    private Entity CreateRuinArch(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("RuinArch", context.BasePosition, variation);
        root.AddChild(visualFactory.CreateBoxEntity("PillarA", EnvironmentMaterialKind.RuinStone, new Vector3(-0.70f, 1.25f, 0f), new Vector3(0.42f, 2.50f, 0.42f)));
        root.AddChild(visualFactory.CreateBoxEntity("PillarB", EnvironmentMaterialKind.RuinStone, new Vector3(0.70f, 1.12f, 0f), new Vector3(0.42f, 2.24f, 0.42f)));
        root.AddChild(visualFactory.CreateBoxEntity("Lintel", EnvironmentMaterialKind.RuinStone, new Vector3(0f, 2.42f, 0f), new Vector3(1.72f, 0.34f, 0.52f)));
        root.AddChild(visualFactory.CreateBoxEntity("BrokenStone", EnvironmentMaterialKind.Stone, new Vector3(1.22f, 0.34f, -0.18f), new Vector3(0.42f, 0.32f, 0.46f), new Vector3(0.14f, 0.20f, 0.10f)));
        return root;
    }

    private Entity CreateDeer(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("Deer", context.BasePosition + new Vector3(0f, 0.18f, 0f), variation);
        root.AddChild(visualFactory.CreateBoxEntity("Body", EnvironmentMaterialKind.Deer, new Vector3(0f, 0.82f, 0f), new Vector3(1.20f, 0.65f, 0.48f)));
        root.AddChild(visualFactory.CreateBoxEntity("Neck", EnvironmentMaterialKind.Deer, new Vector3(0.48f, 1.20f, 0f), new Vector3(0.24f, 0.56f, 0.22f), new Vector3(-0.08f, 0f, 0.18f)));
        root.AddChild(visualFactory.CreateBoxEntity("Head", EnvironmentMaterialKind.Deer, new Vector3(0.70f, 1.44f, 0f), new Vector3(0.46f, 0.28f, 0.24f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegA", EnvironmentMaterialKind.Deer, new Vector3(-0.34f, 0.28f, 0.14f), new Vector3(0.12f, 0.56f, 0.12f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegB", EnvironmentMaterialKind.Deer, new Vector3(0.22f, 0.28f, 0.14f), new Vector3(0.12f, 0.56f, 0.12f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegC", EnvironmentMaterialKind.Deer, new Vector3(-0.34f, 0.28f, -0.14f), new Vector3(0.12f, 0.56f, 0.12f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegD", EnvironmentMaterialKind.Deer, new Vector3(0.22f, 0.28f, -0.14f), new Vector3(0.12f, 0.56f, 0.12f)));
        AttachMotion(root, new Vector3(0f, 0.03f, 0f), new Vector3(0.02f, 0.10f, 0.01f), 0.92f + variation * 0.40f, variation);
        return root;
    }

    private Entity CreateGoat(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("MountainGoat", context.BasePosition + new Vector3(0f, 0.16f, 0f), variation);
        root.AddChild(visualFactory.CreateBoxEntity("Body", EnvironmentMaterialKind.Goat, new Vector3(0f, 0.72f, 0f), new Vector3(0.95f, 0.56f, 0.42f)));
        root.AddChild(visualFactory.CreateBoxEntity("Head", EnvironmentMaterialKind.Goat, new Vector3(0.52f, 1.08f, 0f), new Vector3(0.34f, 0.26f, 0.22f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegA", EnvironmentMaterialKind.Goat, new Vector3(-0.25f, 0.24f, 0.12f), new Vector3(0.10f, 0.48f, 0.10f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegB", EnvironmentMaterialKind.Goat, new Vector3(0.20f, 0.24f, 0.12f), new Vector3(0.10f, 0.48f, 0.10f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegC", EnvironmentMaterialKind.Goat, new Vector3(-0.25f, 0.24f, -0.12f), new Vector3(0.10f, 0.48f, 0.10f)));
        root.AddChild(visualFactory.CreateBoxEntity("LegD", EnvironmentMaterialKind.Goat, new Vector3(0.20f, 0.24f, -0.12f), new Vector3(0.10f, 0.48f, 0.10f)));
        AttachMotion(root, new Vector3(0f, 0.02f, 0f), new Vector3(0.03f, 0.12f, 0.01f), 1.05f + variation * 0.35f, variation);
        return root;
    }

    private Entity CreateGull(PlacementContext context, float variation)
    {
        var root = CreateRootEntity("Gull", context.BasePosition + new Vector3(0f, 0.95f, 0f), variation);
        root.AddChild(visualFactory.CreateBoxEntity("Body", EnvironmentMaterialKind.Gull, new Vector3(0f, 0.20f, 0f), new Vector3(0.42f, 0.20f, 0.22f)));
        root.AddChild(visualFactory.CreateBoxEntity("WingL", EnvironmentMaterialKind.Gull, new Vector3(0f, 0.18f, -0.22f), new Vector3(0.82f, 0.06f, 0.12f), new Vector3(0f, 0f, -0.12f)));
        root.AddChild(visualFactory.CreateBoxEntity("WingR", EnvironmentMaterialKind.Gull, new Vector3(0f, 0.18f, 0.22f), new Vector3(0.82f, 0.06f, 0.12f), new Vector3(0f, 0f, 0.12f)));
        AttachMotion(root, new Vector3(0f, 0.12f, 0f), new Vector3(0.05f, 0.18f, 0.05f), 1.55f + variation * 0.55f, variation);
        return root;
    }

    private Entity CreateRootEntity(string name, Vector3 position, float variation)
    {
        var entity = new Entity(name);
        entity.Transform.Position = position;
        entity.Transform.RotationEulerXYZ = new Vector3(0f, variation * MathF.PI * 2f, 0f);
        return entity;
    }

    private static void AttachMotion(Entity entity, Vector3 positionAmplitude, Vector3 rotationAmplitudeEuler, float speed, float phase)
    {
        entity.Add(new AmbientMotionScript
        {
            PositionAmplitude = positionAmplitude,
            RotationAmplitudeEuler = rotationAmplitudeEuler,
            Speed = speed,
            Phase = phase * MathF.PI * 2f,
        });
    }

    private static float Hash01(int x, int z, int seed)
    {
        int hash = x * 374761393 + z * 668265263 + seed * 1442695041;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash ^= hash >> 16;
        return (hash & 1023) / 1023f;
    }

    private static int HashInt(int x, int z, int seed, int range)
    {
        if (range <= 0)
        {
            return 0;
        }

        return (int)(Hash01(x, z, seed) * range) % range;
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
