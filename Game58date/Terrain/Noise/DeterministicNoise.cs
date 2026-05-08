using System;

namespace Game58date.Terrain.Noise;

public sealed class DeterministicNoise
{
    private readonly int[] permutation;

    public DeterministicNoise(int seed)
    {
        permutation = BuildPermutation(seed);
    }

    public float Fractal2D(float x, float z, int octaves, float frequency, float persistence, float lacunarity)
    {
        float amplitude = 1f;
        float total = 0f;
        float amplitudeSum = 0f;

        for (int octave = 0; octave < octaves; octave++)
        {
            total += Sample2D(x * frequency, z * frequency) * amplitude;
            amplitudeSum += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return amplitudeSum > 0f ? total / amplitudeSum : 0f;
    }

    public float Sample2D(float x, float z)
    {
        int xi = FastFloor(x) & 255;
        int zi = FastFloor(z) & 255;

        float xf = x - FastFloor(x);
        float zf = z - FastFloor(z);

        float u = Fade(xf);
        float v = Fade(zf);

        int aa = permutation[permutation[xi] + zi];
        int ab = permutation[permutation[xi] + zi + 1];
        int ba = permutation[permutation[xi + 1] + zi];
        int bb = permutation[permutation[xi + 1] + zi + 1];

        float x1 = Lerp(Grad2D(aa, xf, zf), Grad2D(ba, xf - 1f, zf), u);
        float x2 = Lerp(Grad2D(ab, xf, zf - 1f), Grad2D(bb, xf - 1f, zf - 1f), u);
        return Lerp(x1, x2, v);
    }

    private static int[] BuildPermutation(int seed)
    {
        var source = new int[256];
        var result = new int[512];
        for (int i = 0; i < 256; i++)
        {
            source[i] = i;
        }

        var random = new Random(seed);
        for (int i = source.Length - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (source[i], source[swapIndex]) = (source[swapIndex], source[i]);
        }

        for (int i = 0; i < 512; i++)
        {
            result[i] = source[i & 255];
        }

        return result;
    }

    private static int FastFloor(float value)
    {
        int integer = (int)value;
        return value < integer ? integer - 1 : integer;
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private static float Grad2D(int hash, float x, float z)
    {
        return (hash & 3) switch
        {
            0 => x + z,
            1 => -x + z,
            2 => x - z,
            _ => -x - z,
        };
    }
}
