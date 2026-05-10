#nullable enable
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class TerrainFaceTextureResolver
{
    public TerrainTextureTile Resolve(BlockKind block, Vector3 faceNormal, bool isFaceExposedToSky)
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

        return block switch
        {
            BlockKind.Stone => isTopFace && isFaceExposedToSky
                ? TerrainTextureTile.MossyStone
                : TerrainTextureTile.Stone,

            BlockKind.Dirt => isTopFace
                ? TerrainTextureTile.RichSoil
                : isBottomFace
                    ? TerrainTextureTile.DrySoil
                    : TerrainTextureTile.Dirt,

            BlockKind.Mud => isTopFace
                ? TerrainTextureTile.Mud
                : isBottomFace
                    ? TerrainTextureTile.Peat
                    : TerrainTextureTile.Mud,

            BlockKind.Peat => isTopFace
                ? TerrainTextureTile.Peat
                : isBottomFace
                    ? TerrainTextureTile.DrySoil
                    : TerrainTextureTile.Peat,

            BlockKind.Moss => isTopFace
                ? TerrainTextureTile.ForestMoss
                : isBottomFace
                    ? TerrainTextureTile.Dirt
                    : TerrainTextureTile.WetGrassSide,

            BlockKind.Snow => isTopFace
                ? TerrainTextureTile.SnowDust
                : isBottomFace
                    ? TerrainTextureTile.Stone
                    : TerrainTextureTile.FrostGrass,

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
                ? TerrainTextureTile.Scree
                : isBottomFace
                    ? TerrainTextureTile.Stone
                    : TerrainTextureTile.Scree,

            _ => TerrainTextureTile.Gravel,
        };
    }
}
