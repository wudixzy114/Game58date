#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Game58date.Terrain;

public sealed class TerrainTextureAtlasFactory
{
    private const int TileSize = 32;
    private const int TileCount = 6;

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

        FillTile(pixels, width, 0, new Color(46, 42, 44), new Color(66, 61, 64), true);
        FillTile(pixels, width, 1, new Color(104, 104, 109), new Color(138, 138, 144), true);
        FillTile(pixels, width, 2, new Color(109, 81, 54), new Color(140, 106, 71), true);
        FillTile(pixels, width, 3, new Color(87, 126, 62), new Color(126, 162, 86), true);
        FillTile(pixels, width, 4, new Color(194, 176, 122), new Color(226, 211, 154), false);
        FillTile(pixels, width, 5, new Color(84, 92, 82), new Color(120, 128, 118), true);

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
