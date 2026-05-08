using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Game58date.Terrain;

public sealed class VoxelChunkMesher
{
    private static readonly FaceDefinition[] Faces =
    {
        new(Vector3.UnitX, new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1)),
        new(-Vector3.UnitX, new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0)),
        new(Vector3.UnitY, new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0)),
        new(-Vector3.UnitY, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1)),
        new(Vector3.UnitZ, new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1)),
        new(-Vector3.UnitZ, new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0)),
    };

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

        for (int y = 0; y < chunk.Height; y++)
        {
            for (int z = 0; z < chunk.Size; z++)
            {
                for (int x = 0; x < chunk.Size; x++)
                {
                    BlockKind block = chunk.GetBlock(x, y, z);
                    if (!IsSolid(block))
                    {
                        continue;
                    }

                    for (int faceIndex = 0; faceIndex < Faces.Length; faceIndex++)
                    {
                        FaceDefinition face = Faces[faceIndex];
                        int sampleX = x + (int)face.Normal.X;
                        int sampleY = y + (int)face.Normal.Y;
                        int sampleZ = z + (int)face.Normal.Z;

                        if (IsSolid(chunk.GetBlock(sampleX, sampleY, sampleZ)))
                        {
                            continue;
                        }

                        AddFace(mesh, block, x, y, z, face, scale);
                        max = Vector3.Max(max, new Vector3((x + 1) * scale, (y + 1) * scale, (z + 1) * scale) + min);
                    }
                }
            }
        }

        if (!mesh.IsEmpty)
        {
            mesh.BoundingBox = new BoundingBox(min, max);
            mesh.BoundingSphere = BoundingSphere.FromBox(mesh.BoundingBox);
        }

        return mesh;
    }

    private static bool IsSolid(BlockKind block)
    {
        return block is not BlockKind.Air and not BlockKind.Water;
    }

    private static Vector2 GetUv(BlockKind block, int cornerIndex)
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

        return cornerIndex switch
        {
            0 => new Vector2(minU, 1f),
            1 => new Vector2(minU, 0f),
            2 => new Vector2(maxU, 0f),
            _ => new Vector2(maxU, 1f),
        };
    }

    private static void AddFace(VoxelChunkMeshData mesh, BlockKind block, int x, int y, int z, FaceDefinition face, float scale)
    {
        int baseVertex = mesh.Vertices.Count;
        Vector3 origin = new(x * scale, y * scale, z * scale);

        mesh.Vertices.Add(new VertexPositionNormalTexture(origin + face.A * scale, face.Normal, GetUv(block, 0)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(origin + face.B * scale, face.Normal, GetUv(block, 1)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(origin + face.C * scale, face.Normal, GetUv(block, 2)));
        mesh.Vertices.Add(new VertexPositionNormalTexture(origin + face.D * scale, face.Normal, GetUv(block, 3)));

        mesh.Indices.Add(baseVertex + 0);
        mesh.Indices.Add(baseVertex + 1);
        mesh.Indices.Add(baseVertex + 2);
        mesh.Indices.Add(baseVertex + 0);
        mesh.Indices.Add(baseVertex + 2);
        mesh.Indices.Add(baseVertex + 3);
    }

    private readonly record struct FaceDefinition(Vector3 Normal, Vector3 A, Vector3 B, Vector3 C, Vector3 D);
}
