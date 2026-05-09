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

            _ => TerrainTextureTile.Gravel,
        };
    }
}
