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
        var mesh = new VoxelChunkMeshData();
        float scale = settings.VoxelScale;
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
                BuildMask(chunk, face, plane, mask, duLimit, dvLimit);
                EmitGreedyQuads(mesh, face, plane, mask, duLimit, dvLimit, scale, ref max);
            }
        }

        if (!mesh.IsEmpty)
        {
            mesh.BoundingBox = new BoundingBox(min, max);
            mesh.BoundingSphere = BoundingSphere.FromBox(mesh.BoundingBox);
        }

        return mesh;
    }

    private static void BuildMask(
        VoxelChunkData chunk,
        FaceDefinition face,
        int plane,
        Span<FaceMaskCell> mask,
        int duLimit,
        int dvLimit)
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

                BlockKind solidBlock = chunk.GetBlock(solid[0], solid[1], solid[2]);
                BlockKind emptyBlock = chunk.GetBlock(empty[0], empty[1], empty[2]);

                mask[index++] = IsSolid(solidBlock) && !IsSolid(emptyBlock)
                    ? new FaceMaskCell(solidBlock)
                    : FaceMaskCell.Empty;
            }
        }
    }

    private static void EmitGreedyQuads(
        VoxelChunkMeshData mesh,
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

                AddQuad(mesh, face, plane, du, dv, width, height, cell.Block, scale, ref max);

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
        VoxelChunkMeshData mesh,
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

        int baseVertex = mesh.Vertices.Count;
        mesh.Vertices.Add(new VertexPositionNormalTexture(p0, face.Normal, GetUv(block, 0, width, height)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(p1, face.Normal, GetUv(block, 1, width, height)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(p2, face.Normal, GetUv(block, 2, width, height)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(p3, face.Normal, GetUv(block, 3, width, height)));

        // Stride's default back-face culling treats clockwise winding as the front face.
        mesh.Indices.Add(baseVertex + 0);
        mesh.Indices.Add(baseVertex + 1);
        mesh.Indices.Add(baseVertex + 2);
        mesh.Indices.Add(baseVertex + 0);
        mesh.Indices.Add(baseVertex + 2);
        mesh.Indices.Add(baseVertex + 3);

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

    private static Vector2 GetUv(BlockKind block, int cornerIndex, int width, int height)
    {
        int tile = block switch
        {
            BlockKind.Bedrock => 0,
            BlockKind.Stone => 1,
            BlockKind.Dirt => 2,
            BlockKind.Grass => 3,
            BlockKind.Sand => 4,
            _ => 5,
        };

        const float tileSize = 1f / 6f;
        float minU = tile * tileSize;
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

    private static bool IsSolid(BlockKind block)
    {
        return block is not BlockKind.Air and not BlockKind.Water;
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
