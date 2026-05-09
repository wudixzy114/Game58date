#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Game58date.Terrain;

public sealed class VoxelChunkMesher
{
    private static readonly FaceDefinition[] Faces =
    {
        new(0, 1, 2, 1, Vector3.UnitX),
        new(0, -1, 1, 2, -Vector3.UnitX),
        new(1, 1, 0, 2, Vector3.UnitY),
        new(1, -1, 2, 0, -Vector3.UnitY),
        new(2, 1, 1, 0, Vector3.UnitZ),
        new(2, -1, 0, 1, -Vector3.UnitZ),
    };

    private readonly TerrainGenerationSettings settings;
    private readonly TerrainFaceTextureResolver faceTextureResolver = new();

    static VoxelChunkMesher()
    {
        ValidateFaceDefinitions();
    }

    public VoxelChunkMesher(TerrainGenerationSettings settings)
    {
        this.settings = settings;
    }

    public VoxelChunkMeshData Build(VoxelChunkData chunk)
    {
        return Build(chunk, null);
    }

    public VoxelChunkMeshData Build(VoxelChunkData chunk, Func<int, int, int, BlockKind>? sampleBlockWorld)
    {
        var mesh = new VoxelChunkMeshData();
        float scale = settings.VoxelScale;
        BuildSurfaceMesh(chunk, mesh.Solid, scale, meshingWater: false, sampleBlockWorld);
        BuildSurfaceMesh(chunk, mesh.Water, scale, meshingWater: true, sampleBlockWorld);

        return mesh;
    }

    private void BuildSurfaceMesh(VoxelChunkData chunk, VoxelSurfaceMeshData surface, float scale, bool meshingWater, Func<int, int, int, BlockKind>? sampleBlockWorld)
    {
        Vector3 min = Vector3.Zero;
        Vector3 max = min;

        int[] dims = { chunk.Size, chunk.Height, chunk.Size };
        Span<FaceMaskCell> mask = stackalloc FaceMaskCell[Math.Max(chunk.Size * chunk.Height, Math.Max(chunk.Size * chunk.Size, chunk.Height * chunk.Size))];

        foreach (FaceDefinition face in Faces)
        {
            int duLimit = dims[face.UAxis];
            int dvLimit = dims[face.VAxis];
            int planeLimit = dims[face.Axis];

            for (int plane = 0; plane <= planeLimit; plane++)
            {
                BuildMask(chunk, face, plane, mask, duLimit, dvLimit, meshingWater, sampleBlockWorld);
                EmitGreedyQuads(chunk, surface, face, plane, mask, duLimit, dvLimit, scale, ref max);
            }
        }

        if (!surface.IsEmpty)
        {
            surface.BoundingBox = new BoundingBox(min, max);
            surface.BoundingSphere = BoundingSphere.FromBox(surface.BoundingBox);
        }
    }

    private static void BuildMask(
        VoxelChunkData chunk,
        FaceDefinition face,
        int plane,
        Span<FaceMaskCell> mask,
        int duLimit,
        int dvLimit,
        bool meshingWater,
        Func<int, int, int, BlockKind>? sampleBlockWorld)
    {
        int index = 0;

        for (int dv = 0; dv < dvLimit; dv++)
        {
            for (int du = 0; du < duLimit; du++)
            {
                int[] solid = new int[3];
                int[] empty = new int[3];

                solid[face.Axis] = face.NormalSign > 0 ? plane - 1 : plane;
                empty[face.Axis] = face.NormalSign > 0 ? plane : plane - 1;

                solid[face.UAxis] = du;
                empty[face.UAxis] = du;
                solid[face.VAxis] = dv;
                empty[face.VAxis] = dv;

                BlockKind solidBlock = SampleBlock(chunk, solid[0], solid[1], solid[2], sampleBlockWorld);
                BlockKind emptyBlock = SampleBlock(chunk, empty[0], empty[1], empty[2], sampleBlockWorld);

                bool shouldEmit = meshingWater
                    ? IsWater(solidBlock) && IsAir(emptyBlock)
                    : IsSolid(solidBlock) && !IsSolid(emptyBlock) && !IsWater(emptyBlock);

                mask[index++] = shouldEmit
                    ? new FaceMaskCell(solidBlock)
                    : FaceMaskCell.Empty;
            }
        }
    }

    private void EmitGreedyQuads(
        VoxelChunkData chunk,
        VoxelSurfaceMeshData surface,
        FaceDefinition face,
        int plane,
        Span<FaceMaskCell> mask,
        int duLimit,
        int dvLimit,
        float scale,
        ref Vector3 max)
    {
        for (int dv = 0; dv < dvLimit; dv++)
        {
            int du = 0;
            while (du < duLimit)
            {
                int index = dv * duLimit + du;
                FaceMaskCell cell = mask[index];
                if (!cell.IsVisible)
                {
                    du++;
                    continue;
                }

                int width = 1;
                while (du + width < duLimit && mask[index + width] == cell)
                {
                    width++;
                }

                int height = 1;
                bool canGrow = true;
                while (dv + height < dvLimit && canGrow)
                {
                    for (int offset = 0; offset < width; offset++)
                    {
                        if (mask[(dv + height) * duLimit + du + offset] != cell)
                        {
                            canGrow = false;
                            break;
                        }
                    }

                    if (canGrow)
                    {
                        height++;
                    }
                }

                AddQuad(surface, faceTextureResolver, chunk, face, plane, du, dv, width, height, cell.Block, scale, ref max);

                for (int clearV = 0; clearV < height; clearV++)
                {
                    for (int clearU = 0; clearU < width; clearU++)
                    {
                        mask[(dv + clearV) * duLimit + du + clearU] = FaceMaskCell.Empty;
                    }
                }

                du += width;
            }
        }
    }

    private static void AddQuad(
        VoxelSurfaceMeshData surface,
        TerrainFaceTextureResolver faceTextureResolver,
        VoxelChunkData chunk,
        FaceDefinition face,
        int plane,
        int du,
        int dv,
        int width,
        int height,
        BlockKind block,
        float scale,
        ref Vector3 max)
    {
        Vector3 p0 = BuildPosition(face, plane, du, dv, scale);
        Vector3 p1 = BuildPosition(face, plane, du + width, dv, scale);
        Vector3 p2 = BuildPosition(face, plane, du + width, dv + height, scale);
        Vector3 p3 = BuildPosition(face, plane, du, dv + height, scale);

        bool isFaceExposedToSky = IsFaceExposedToSky(chunk, face, plane, du, dv, width, height);
        TerrainTextureTile tile = faceTextureResolver.Resolve(block, face.Normal, isFaceExposedToSky);

        int baseVertex = surface.Vertices.Count;
        surface.Vertices.Add(new VertexPositionNormalTexture(p0, face.Normal, GetUv(tile, 0, width, height)));
        surface.Vertices.Add(new VertexPositionNormalTexture(p1, face.Normal, GetUv(tile, 1, width, height)));
        surface.Vertices.Add(new VertexPositionNormalTexture(p2, face.Normal, GetUv(tile, 2, width, height)));
        surface.Vertices.Add(new VertexPositionNormalTexture(p3, face.Normal, GetUv(tile, 3, width, height)));

        // Stride's default back-face culling treats clockwise winding as the front face.
        surface.Indices.Add(baseVertex + 0);
        surface.Indices.Add(baseVertex + 1);
        surface.Indices.Add(baseVertex + 2);
        surface.Indices.Add(baseVertex + 0);
        surface.Indices.Add(baseVertex + 2);
        surface.Indices.Add(baseVertex + 3);

        max = Vector3.Max(max, p0);
        max = Vector3.Max(max, p1);
        max = Vector3.Max(max, p2);
        max = Vector3.Max(max, p3);
    }

    private static Vector3 BuildPosition(FaceDefinition face, int plane, int du, int dv, float scale)
    {
        Vector3 position = Vector3.Zero;
        position[face.Axis] = plane * scale;
        position[face.UAxis] = du * scale;
        position[face.VAxis] = dv * scale;
        return position;
    }

    private static Vector2 GetUv(TerrainTextureTile tile, int cornerIndex, int width, int height)
    {
        const float atlasTileCount = 12f;
        float tileSize = 1f / atlasTileCount;
        float minU = (int)tile * tileSize;
        float maxU = minU + tileSize;
        float repeatV = Math.Max(1, height);

        return cornerIndex switch
        {
            0 => new Vector2(minU, repeatV),
            1 => new Vector2(minU, 0f),
            2 => new Vector2(maxU, 0f),
            _ => new Vector2(maxU, repeatV),
        };
    }

    private static bool IsFaceExposedToSky(VoxelChunkData chunk, FaceDefinition face, int plane, int du, int dv, int width, int height)
    {
        if (face.Normal.Y <= 0f)
        {
            return false;
        }

        int localY = plane - 1;
        if (localY < 0)
        {
            return false;
        }

        for (int offsetV = 0; offsetV < height; offsetV++)
        {
            for (int offsetU = 0; offsetU < width; offsetU++)
            {
                int[] point = new int[3];
                point[face.UAxis] = du + offsetU;
                point[face.VAxis] = dv + offsetV;
                point[face.Axis] = localY;

                for (int y = point[1] + 1; y < chunk.Height; y++)
                {
                    if (chunk.GetBlock(point[0], y, point[2]) is not BlockKind.Air and not BlockKind.Water)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool IsSolid(BlockKind block)
    {
        return block is not BlockKind.Air and not BlockKind.Water;
    }

    private static bool IsWater(BlockKind block)
    {
        return block == BlockKind.Water;
    }

    private static bool IsAir(BlockKind block)
    {
        return block == BlockKind.Air;
    }

    private static BlockKind SampleBlock(VoxelChunkData chunk, int localX, int localY, int localZ, Func<int, int, int, BlockKind>? sampleBlockWorld)
    {
        if ((uint)localX < (uint)chunk.Size && (uint)localZ < (uint)chunk.Size && (uint)localY < (uint)chunk.Height)
        {
            return chunk.GetBlock(localX, localY, localZ);
        }

        if (sampleBlockWorld is null)
        {
            return BlockKind.Air;
        }

        int worldX = chunk.Coordinate.X * chunk.Size + localX;
        int worldZ = chunk.Coordinate.Z * chunk.Size + localZ;
        return sampleBlockWorld(worldX, localY, worldZ);
    }

    private static void ValidateFaceDefinitions()
    {
        foreach (FaceDefinition face in Faces)
        {
            Vector3 p0 = BuildPosition(face, 1, 0, 0, 1f);
            Vector3 p1 = BuildPosition(face, 1, 1, 0, 1f);
            Vector3 p2 = BuildPosition(face, 1, 1, 1, 1f);

            Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
            float alignment = Vector3.Dot(cross, face.Normal);
            if (alignment >= 0f)
            {
                throw new InvalidOperationException($"Face definition winding is invalid for axis={face.Axis}, sign={face.NormalSign}.");
            }
        }
    }

    private readonly record struct FaceDefinition(int Axis, int NormalSign, int UAxis, int VAxis, Vector3 Normal);

    private readonly record struct FaceMaskCell(BlockKind Block)
    {
        public static FaceMaskCell Empty => new(BlockKind.Air);

        public bool IsVisible => Block != BlockKind.Air;
    }
}
