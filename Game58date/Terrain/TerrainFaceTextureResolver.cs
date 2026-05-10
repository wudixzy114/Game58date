#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class TerrainFaceTextureResolver
{
    public TerrainTextureTile Resolve(BlockKind block, Vector3 faceNormal, bool isFaceExposedToSky, int worldX, int worldY, int worldZ)
    {
        if (block == BlockKind.Water)
        {
            return TerrainTextureTile.Gravel;
        }

        if (block == BlockKind.Bedrock)
        {
            return TerrainTextureTile.Bedrock;
        }

        bool isTopFace = faceNormal.Y > 0.5f;
        bool isBottomFace = faceNormal.Y < -0.5f;
        float variation = Hash01(worldX, worldY, worldZ, 991);

        return block switch
        {
            BlockKind.Stone => ResolveStoneTile(isTopFace, isFaceExposedToSky, variation),

            BlockKind.Dirt => isTopFace
                ? variation < 0.46f
                    ? TerrainTextureTile.RichSoil
                    : TerrainTextureTile.RootSoil
                : isBottomFace
                    ? TerrainTextureTile.DrySoil
                    : TerrainTextureTile.Dirt,

            BlockKind.Mud => isTopFace
                ? variation < 0.58f
                    ? TerrainTextureTile.Mud
                    : TerrainTextureTile.WetMud
                : isBottomFace
                    ? variation < 0.52f
                        ? TerrainTextureTile.Peat
                        : TerrainTextureTile.ColdPeat
                    : variation < 0.54f
                        ? TerrainTextureTile.Mud
                        : TerrainTextureTile.WetMud,

            BlockKind.Peat => isTopFace
                ? variation < 0.48f
                    ? TerrainTextureTile.Peat
                    : TerrainTextureTile.ColdPeat
                : isBottomFace
                    ? TerrainTextureTile.DrySoil
                    : variation < 0.48f
                        ? TerrainTextureTile.Peat
                        : TerrainTextureTile.ColdPeat,

            BlockKind.Moss => isTopFace
                ? variation < 0.52f
                    ? TerrainTextureTile.ForestMoss
                    : TerrainTextureTile.DarkForestMoss
                : isBottomFace
                    ? TerrainTextureTile.Dirt
                    : TerrainTextureTile.WetGrassSide,

            BlockKind.Snow => isTopFace
                ? variation < 0.54f
                    ? TerrainTextureTile.SnowDust
                    : TerrainTextureTile.BlueSnow
                : isBottomFace
                    ? TerrainTextureTile.Stone
                    : variation < 0.46f
                        ? TerrainTextureTile.FrostGrass
                        : TerrainTextureTile.TundraSoil,

            BlockKind.Grass => isTopFace
                ? TerrainTextureTile.GrassTop
                : isBottomFace
                    ? TerrainTextureTile.Dirt
                    : TerrainTextureTile.GrassSide,

            BlockKind.Sand => isTopFace
                ? TerrainTextureTile.Sand
                : isBottomFace
                    ? TerrainTextureTile.Sandstone
                    : TerrainTextureTile.Sand,

            BlockKind.Scree => isTopFace
                ? variation < 0.50f
                    ? TerrainTextureTile.Scree
                    : TerrainTextureTile.BrokenScree
                : isBottomFace
                    ? TerrainTextureTile.Stone
                    : variation < 0.50f
                        ? TerrainTextureTile.Scree
                        : TerrainTextureTile.BrokenScree,

            _ => TerrainTextureTile.Gravel,
        };
    }

    private static TerrainTextureTile ResolveStoneTile(bool isTopFace, bool isFaceExposedToSky, float variation)
    {
        if (isTopFace && isFaceExposedToSky)
        {
            return variation < 0.38f
                ? TerrainTextureTile.MossyStone
                : TerrainTextureTile.WeatheredStone;
        }

        return variation > 0.82f
            ? TerrainTextureTile.WeatheredStone
            : TerrainTextureTile.Stone;
    }

    private static float Hash01(int x, int y, int z, int seed)
    {
        int hash = x * 374761393 + y * 668265263 + z * 2147385601 + seed * 1442695041;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash ^= hash >> 16;
        return (hash & 1023) / 1023f;
    }
}
