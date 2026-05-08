using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Game58date.Terrain;

public sealed class VoxelChunkMesher
{
    private readonly TerrainGenerationSettings settings;

    public VoxelChunkMesher(TerrainGenerationSettings settings)
    {
        this.settings = settings;
    }

    public VoxelChunkMeshData Build(VoxelChunkData chunk)
    {
        var mesh = new VoxelChunkMeshData();
        float scale = settings.VoxelScale;
        Vector3 min = new(
            chunk.Coordinate.X * chunk.Size * scale,
            0f,
            chunk.Coordinate.Z * chunk.Size * scale);
        Vector3 max = min;

        int[] dims = { chunk.Size, chunk.Height, chunk.Size };
        Span<FaceMaskCell> mask = stackalloc FaceMaskCell[Math.Max(chunk.Size * chunk.Height, Math.Max(chunk.Size * chunk.Size, chunk.Height * chunk.Size))];

        for (int axis = 0; axis < 3; axis++)
        {
            int u = (axis + 1) % 3;
            int v = (axis + 2) % 3;
            int duLimit = dims[u];
            int dvLimit = dims[v];

            for (int slice = -1; slice < dims[axis]; slice++)
            {
                BuildMask(chunk, axis, slice, dims, mask, duLimit, dvLimit);
                EmitGreedyQuads(mesh, mask, axis, slice, dims, duLimit, dvLimit, scale, ref max);
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
        int axis,
        int slice,
        int[] dims,
        Span<FaceMaskCell> mask,
        int duLimit,
        int dvLimit)
    {
        int u = (axis + 1) % 3;
        int v = (axis + 2) % 3;
        int index = 0;

        for (int dv = 0; dv < dvLimit; dv++)
        {
            for (int du = 0; du < duLimit; du++)
            {
                int[] near = new int[3];
                int[] far = new int[3];

                near[axis] = slice;
                far[axis] = slice + 1;
                near[u] = du;
                far[u] = du;
                near[v] = dv;
                far[v] = dv;

                BlockKind nearBlock = GetBlock(chunk, near[0], near[1], near[2]);
                BlockKind farBlock = GetBlock(chunk, far[0], far[1], far[2]);

                bool nearSolid = IsSolid(nearBlock);
                bool farSolid = IsSolid(farBlock);

                mask[index++] = nearSolid == farSolid
                    ? FaceMaskCell.Empty
                    : nearSolid
                        ? new FaceMaskCell(nearBlock, -1)
                        : new FaceMaskCell(farBlock, 1);
            }
        }
    }

    private void EmitGreedyQuads(
        VoxelChunkMeshData mesh,
        Span<FaceMaskCell> mask,
        int axis,
        int slice,
        int[] dims,
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
                bool keepGrowing = true;
                while (dv + height < dvLimit && keepGrowing)
                {
                    for (int offset = 0; offset < width; offset++)
                    {
                        if (mask[(dv + height) * duLimit + du + offset] != cell)
                        {
                            keepGrowing = false;
                            break;
                        }
                    }

                    if (keepGrowing)
                    {
                        height++;
                    }
                }

                AddQuad(mesh, axis, slice, du, dv, width, height, cell, scale, ref max);

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
        int axis,
        int slice,
        int du,
        int dv,
        int width,
        int height,
        FaceMaskCell cell,
        float scale,
        ref Vector3 max)
    {
        int u = (axis + 1) % 3;
        int v = (axis + 2) % 3;
        int plane = cell.NormalSign > 0 ? slice + 1 : slice + 1;

        Vector3 basePosition = Vector3.Zero;
        basePosition[axis] = plane * scale;
        basePosition[u] = du * scale;
        basePosition[v] = dv * scale;

        Vector3 duVector = Vector3.Zero;
        duVector[u] = width * scale;
        Vector3 dvVector = Vector3.Zero;
        dvVector[v] = height * scale;

        Vector3 normal = Vector3.Zero;
        normal[axis] = cell.NormalSign;

        int baseVertex = mesh.Vertices.Count;
        Vector3 p0;
        Vector3 p1;
        Vector3 p2;
        Vector3 p3;

        if (cell.NormalSign > 0)
        {
            p0 = basePosition;
            p1 = basePosition + dvVector;
            p2 = basePosition + duVector + dvVector;
            p3 = basePosition + duVector;
        }
        else
        {
            p0 = basePosition;
            p1 = basePosition + duVector;
            p2 = basePosition + duVector + dvVector;
            p3 = basePosition + dvVector;
        }

        mesh.Vertices.Add(new VertexPositionNormalTexture(p0, normal, GetUv(cell.Block, 0, width, height)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(p1, normal, GetUv(cell.Block, 1, width, height)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(p2, normal, GetUv(cell.Block, 2, width, height)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(p3, normal, GetUv(cell.Block, 3, width, height)));

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
        float repeatU = Math.Max(1, width);
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

    private static BlockKind GetBlock(VoxelChunkData chunk, int x, int y, int z)
    {
        return chunk.GetBlock(x, y, z);
    }

    private readonly record struct FaceMaskCell(BlockKind Block, int NormalSign)
    {
        public static FaceMaskCell Empty => new(BlockKind.Air, 0);

        public bool IsVisible => NormalSign != 0;
    }
}
