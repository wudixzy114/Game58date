#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Game58date.Terrain;

public sealed class TerrainTextureAtlasFactory
{
    private const int TileSize = 64;
    private const int TileCount = 12;

    private readonly GraphicsDevice graphicsDevice;
    private readonly GraphicsContext graphicsContext;
    private Texture? atlas;

    public TerrainTextureAtlasFactory(GraphicsDevice graphicsDevice, GraphicsContext graphicsContext)
    {
        this.graphicsDevice = graphicsDevice;
        this.graphicsContext = graphicsContext;
    }

    public Texture GetOrCreate()
    {
        atlas ??= BuildAtlas();
        return atlas;
    }

    private Texture BuildAtlas()
    {
        int width = TileSize * TileCount;
        int height = TileSize;
        var pixels = new Color[width * height];

        FillBedrockTile(pixels, width, (int)TerrainTextureTile.Bedrock);
        FillRockTile(pixels, width, (int)TerrainTextureTile.Stone, new Color(92, 94, 98), new Color(136, 139, 145), 0.82f);
        FillRockTile(pixels, width, (int)TerrainTextureTile.Cliff, new Color(108, 104, 94), new Color(150, 145, 132), 0.88f);
        FillSoilTile(pixels, width, (int)TerrainTextureTile.Dirt, new Color(101, 75, 52), new Color(138, 104, 72), 0.12f);
        FillGrassTopTile(pixels, width, (int)TerrainTextureTile.GrassTop);
        FillGrassSideTile(pixels, width, (int)TerrainTextureTile.GrassSide);
        FillSandTile(pixels, width, (int)TerrainTextureTile.Sand, new Color(198, 181, 126), new Color(229, 214, 161));
        FillSandTile(pixels, width, (int)TerrainTextureTile.Sandstone, new Color(171, 143, 99), new Color(208, 183, 131));
        FillPebbleTile(pixels, width, (int)TerrainTextureTile.Gravel, new Color(104, 108, 102), new Color(142, 146, 136));
        FillSoilTile(pixels, width, (int)TerrainTextureTile.DrySoil, new Color(126, 97, 64), new Color(159, 125, 87), 0.05f);
        FillRockTile(pixels, width, (int)TerrainTextureTile.MossyStone, new Color(86, 97, 84), new Color(128, 140, 120), 0.65f);
        FillSoilTile(pixels, width, (int)TerrainTextureTile.RichSoil, new Color(82, 58, 38), new Color(119, 85, 56), 0.20f);

        Color[][] mipChain = BuildMipChain(width, height, pixels);
        Texture texture = Texture.New2D(
            graphicsDevice,
            width,
            height,
            true,
            PixelFormat.R8G8B8A8_UNorm,
            TextureFlags.ShaderResource,
            1,
            GraphicsResourceUsage.Default,
            TextureOptions.None);

        for (int mipLevel = 0; mipLevel < mipChain.Length; mipLevel++)
        {
            texture.SetData(graphicsContext.CommandList, mipChain[mipLevel], 0, mipLevel, null);
        }

        return texture;
    }

    private static Color[][] BuildMipChain(int width, int height, Color[] basePixels)
    {
        int mipCount = Texture.CalculateMipLevels(width, height, true);
        var mipPixels = new Color[mipCount][];
        mipPixels[0] = basePixels;

        int currentWidth = width;
        int currentHeight = height;
        Color[] currentLevel = basePixels;

        for (int mipLevel = 1; mipLevel < mipCount; mipLevel++)
        {
            int nextWidth = Math.Max(1, currentWidth / 2);
            int nextHeight = Math.Max(1, currentHeight / 2);
            var nextLevel = new Color[nextWidth * nextHeight];

            for (int y = 0; y < nextHeight; y++)
            {
                for (int x = 0; x < nextWidth; x++)
                {
                    nextLevel[y * nextWidth + x] = AverageBlock(currentLevel, currentWidth, currentHeight, x * 2, y * 2);
                }
            }

            mipPixels[mipLevel] = nextLevel;
            currentLevel = nextLevel;
            currentWidth = nextWidth;
            currentHeight = nextHeight;
        }

        return mipPixels;
    }

    private static Color AverageBlock(Color[] pixels, int width, int height, int startX, int startY)
    {
        int sampleCount = 0;
        int r = 0;
        int g = 0;
        int b = 0;

        for (int dy = 0; dy < 2; dy++)
        {
            int y = Math.Min(startY + dy, height - 1);
            for (int dx = 0; dx < 2; dx++)
            {
                int x = Math.Min(startX + dx, width - 1);
                Color color = pixels[y * width + x];
                r += color.R;
                g += color.G;
                b += color.B;
                sampleCount++;
            }
        }

        return new Color(
            (byte)(r / sampleCount),
            (byte)(g / sampleCount),
            (byte)(b / sampleCount),
            255);
    }

    private static void FillTile(Color[] pixels, int atlasWidth, int tileIndex, Color dark, Color light, bool rockPattern)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float noiseA = Noise(x, y, tileIndex * 11 + 3);
                float noiseB = Noise(x * 2, y * 2, tileIndex * 17 + 7);
                float blend = rockPattern ? noiseA * 0.65f + noiseB * 0.35f : noiseA * 0.35f + noiseB * 0.15f;
                if (!rockPattern)
                {
                    blend += (1f - MathF.Abs(y - TileSize * 0.5f) / (TileSize * 0.5f)) * 0.18f;
                }

                pixels[y * atlasWidth + xOffset + x] = Lerp(dark, light, Math.Clamp(blend, 0f, 1f));
            }
        }
    }

    private static void FillBedrockTile(Color[] pixels, int atlasWidth, int tileIndex)
    {
        FillTile(pixels, atlasWidth, tileIndex, new Color(34, 31, 35), new Color(60, 57, 63), true);
        OverlayCracks(pixels, atlasWidth, tileIndex, new Color(18, 17, 20), spacing: 9, intensity: 0.32f);
    }

    private static void FillRockTile(Color[] pixels, int atlasWidth, int tileIndex, Color dark, Color light, float ridgeStrength)
    {
        FillTile(pixels, atlasWidth, tileIndex, dark, light, true);
        OverlayRidges(pixels, atlasWidth, tileIndex, ridgeStrength);
    }

    private static void FillSoilTile(Color[] pixels, int atlasWidth, int tileIndex, Color dark, Color light, float grainBoost)
    {
        FillTile(pixels, atlasWidth, tileIndex, dark, light, false);
        OverlaySoilGrain(pixels, atlasWidth, tileIndex, grainBoost);
    }

    private static void FillGrassTopTile(Color[] pixels, int atlasWidth, int tileIndex)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float bladeNoise = Noise(x * 3, y * 2, 1803);
                float macroNoise = Noise(x, y, 1819);
                float floralNoise = Noise(x * 5, y * 5, 1847);
                float green = Math.Clamp(0.35f + bladeNoise * 0.45f + macroNoise * 0.20f, 0f, 1f);
                Color baseColor = Lerp(new Color(65, 99, 42), new Color(131, 170, 85), green);
                if (floralNoise > 0.94f)
                {
                    baseColor = Lerp(baseColor, new Color(204, 196, 120), 0.45f);
                }

                pixels[y * atlasWidth + xOffset + x] = baseColor;
            }
        }
    }

    private static void FillGrassSideTile(Color[] pixels, int atlasWidth, int tileIndex)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            float heightRatio = y / (float)(TileSize - 1);
            for (int x = 0; x < TileSize; x++)
            {
                float grassBand = Math.Clamp(1f - heightRatio * 2.8f, 0f, 1f);
                float grassNoise = Noise(x * 2, y * 2, 1901);
                float soilNoise = Noise(x, y, 1913);

                Color soil = Lerp(new Color(98, 73, 50), new Color(138, 106, 72), soilNoise * 0.75f);
                Color grass = Lerp(new Color(68, 106, 44), new Color(126, 162, 84), grassNoise);
                pixels[y * atlasWidth + xOffset + x] = Lerp(soil, grass, grassBand);
            }
        }
    }

    private static void FillSandTile(Color[] pixels, int atlasWidth, int tileIndex, Color dark, Color light)
    {
        FillTile(pixels, atlasWidth, tileIndex, dark, light, false);
        OverlayWindRipples(pixels, atlasWidth, tileIndex);
    }

    private static void FillPebbleTile(Color[] pixels, int atlasWidth, int tileIndex, Color dark, Color light)
    {
        FillTile(pixels, atlasWidth, tileIndex, dark, light, true);
        OverlayPebbles(pixels, atlasWidth, tileIndex);
    }

    private static void OverlayCracks(Color[] pixels, int atlasWidth, int tileIndex, Color crackColor, int spacing, float intensity)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float crack = MathF.Abs(((x + y * 2) % spacing) - spacing * 0.5f) / (spacing * 0.5f);
                if (crack < 0.18f && Noise(x, y, tileIndex * 71 + 11) > 0.42f)
                {
                    int index = y * atlasWidth + xOffset + x;
                    pixels[index] = Lerp(pixels[index], crackColor, intensity * (1f - crack / 0.18f));
                }
            }
        }
    }

    private static void OverlayRidges(Color[] pixels, int atlasWidth, int tileIndex, float ridgeStrength)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float ridge = Noise(x * 4, y * 4, tileIndex * 97 + 23);
                if (ridge > 0.78f)
                {
                    int index = y * atlasWidth + xOffset + x;
                    pixels[index] = Lerp(pixels[index], new Color(202, 202, 196), (ridge - 0.78f) * ridgeStrength);
                }
            }
        }
    }

    private static void OverlaySoilGrain(Color[] pixels, int atlasWidth, int tileIndex, float grainBoost)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float grain = Noise(x * 6, y * 6, tileIndex * 131 + 17);
                float amount = MathF.Max(0f, grain - 0.62f) * (0.35f + grainBoost);
                if (amount <= 0f)
                {
                    continue;
                }

                int index = y * atlasWidth + xOffset + x;
                pixels[index] = Lerp(pixels[index], new Color(182, 144, 96), amount);
            }
        }
    }

    private static void OverlayWindRipples(Color[] pixels, int atlasWidth, int tileIndex)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float ripple = MathF.Sin((x + y * 0.35f) * 0.42f) * 0.5f + 0.5f;
                int index = y * atlasWidth + xOffset + x;
                pixels[index] = ripple > 0.55f
                    ? Lerp(pixels[index], new Color(241, 229, 181), (ripple - 0.55f) * 0.18f)
                    : pixels[index];
            }
        }
    }

    private static void OverlayPebbles(Color[] pixels, int atlasWidth, int tileIndex)
    {
        int xOffset = tileIndex * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float pebbleNoise = Noise(x * 5, y * 5, tileIndex * 173 + 29);
                if (pebbleNoise <= 0.90f)
                {
                    continue;
                }

                int index = y * atlasWidth + xOffset + x;
                pixels[index] = Lerp(pixels[index], new Color(188, 190, 182), (pebbleNoise - 0.90f) * 0.65f);
            }
        }
    }

    private static float Noise(int x, int y, int seed)
    {
        int hash = x * 374761393 + y * 668265263 + seed * 1442695041;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash ^= hash >> 16;
        return (hash & 1023) / 1023f;
    }

    private static Color Lerp(Color from, Color to, float amount)
    {
        byte r = (byte)(from.R + (to.R - from.R) * amount);
        byte g = (byte)(from.G + (to.G - from.G) * amount);
        byte b = (byte)(from.B + (to.B - from.B) * amount);
        return new Color(r, g, b, 255);
    }
}
