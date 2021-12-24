
using System.Xml;
using ProceduralTerrain;
using UnityEngine;
using UnityEngine.Profiling;

/*
 * Original implementation, by Ken Perlin: https://cs.nyu.edu/~perlin/noise/
 * Paper detailing the improvements: https://weber.itn.liu.se/~stegu/TNM022-2005/perlinnoiselinks/paper445.pdf
 * First implementation was created with the help of: http://adrianb.io/2014/08/09/perlinnoise.html
 * Reimplementation for 2D and 3D, more unity-friendly, inspired by: https://catlikecoding.com/unity/tutorials/noise/
 */
public class PerlinNoise
{
    public static NoiseManager settings = NoiseManager.Instance;
    public static int Seed { get; set; } = 0;
    public static int Octaves { get; set; } = 1;
    public static float Frequency { get; set; } = 1f;
    public static float Gain { get; set; } = 2f;
    public static float Lacunarity { get; set; } = 0.5f;
    public static int[] Permutation2X { get; private set; }
    
    // This will be useful later, trust me.
    private static readonly float Sqr2 = Mathf.Sqrt(2f);

    // Hash lookup table as defined by Ken Perlin in https://cs.nyu.edu/~perlin/noise/
    private static readonly int[] Permutation = { 151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    // Initialize permutation to avoid overflow issues
    static void Init()
    {
        Permutation2X = new int[512];
        for (uint i = 0; i < 512; i++)
            Permutation2X[i] = Permutation[i % 256];
    }
    
    public static float Fractal2D(Vector2 point, Vector2 offset)
    {
        float range = 1.0f;
        point += new Vector2(settings.Offset.x, settings.Offset.z) + offset;
        point *= settings.Scale;
        
        float freq = settings.Frequency;
        float amp = settings.Amplitude;
        float value = Perlin2D(point, freq);
        for (int i = 1; i < settings.Octaves; i++)
        {
            freq *= settings.Lacunarity;
            amp *= settings.Gain;
            range += amp;
            value += amp * Perlin2D(point, freq);
        }

        value /= range;
        value = Mathf.Clamp(value, -1f, 1f);
        return Mathf.Pow(value*0.5f + 0.5f, settings.Exponent) * settings.Multiplier;
    }
    
    public static float Fractal3D(Vector3 point, float scale)
    {
        return Perlin3D(point * scale);
    }

    public static float Perlin3D(Vector3 point)
    {
        Init();
        int xi = Mathf.FloorToInt(point.x);
        int yi = Mathf.FloorToInt(point.y);
        int zi = Mathf.FloorToInt(point.z);

        // Map coordinates to the "unit cube" that contains the desired point.
        float xf = point.x - xi;
        float yf = point.y - yi;
        float zf = point.z - zi;
        
        // Finding the actual unit cube
        // Binding coordinates to [0, 255] to prevent overflowing when accessing our permutation table. 
        // This makes the noise repeat every 256 coords, but decimal coordinates are possible so it's not an issue.
        xi &= 255;
        yi &= 255;
        zi &= 255;

        // Compute the fade (smoothing) curves for each mapped coordinate
        float u = Fade(xf);
        float v = Fade(yf);
        float w = Fade(zf);
        
        // Get the right gradient for each vertex of the cube (just does a lookup in the static array).
        Vector3 g000 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[xi] + yi] + zi] & 15];
        Vector3 g100 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[(xi + 1)] + yi] + zi] & 15];
        Vector3 g010 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[xi] + (yi + 1)] + zi] & 15];
        Vector3 g110 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[(xi + 1)] + (yi + 1)] + zi] & 15];
        Vector3 g001 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[xi] + yi] + (zi + 1)] & 15];
        Vector3 g011 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[xi] + (yi + 1)] + (zi + 1)] & 15];
        Vector3 g101 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[(xi + 1)] + yi] + (zi + 1)] & 15];
        Vector3 g111 = _gradients3D[Permutation2X[Permutation2X[Permutation2X[(xi + 1)] + (yi + 1)] + (zi + 1)] & 15];
        
        /*
         * Now finally compute the dot products between the gradients and the corresponding coordinates!
         * In Perlin's original implementation, the function is more compact and messy. Instead of actually doing the
         * dot product, he takes as input the hashed indexes and returns the "already computed" dot product through a
         * series of bitwise checks (e.g. if the bitwise checks would result in a gradient of (1, 1, 0), the original
         * function simply returned xf + yf). This achieves the same goal, but is clearer and more C# friendly.
         *
         * e.g. 
         */
        float d000 = Vector3.Dot(g000, new Vector3(xf, yf, zf));
        float d100 = Vector3.Dot(g100, new Vector3(xf - 1, yf, zf));
        float d010 = Vector3.Dot(g010, new Vector3(xf, yf - 1, zf));
        float d110 = Vector3.Dot(g110, new Vector3(xf - 1, yf - 1, zf));
        float d001 = Vector3.Dot(g001, new Vector3(xf, yf, zf - 1));
        float d011 = Vector3.Dot(g011, new Vector3(xf, yf - 1, zf - 1));
        float d101 = Vector3.Dot(g101, new Vector3(xf - 1, yf, zf - 1));
        float d111 = Vector3.Dot(g111, new Vector3(xf - 1, yf - 1, zf - 1));

        /*
         Now we do the linear interpolation of the obtained values, while using the quintic smoothing curves that
         were computed earlier. The interpolation has 3 "layers": the first interpolates the dot products 2 by 2
         while using the curve obtained from xf; the second interpolates the results of the first using the curve from
         yf; and the last interpolates the results of the second using the curve from zf.
         */
        return Mathf.Lerp(
            Mathf.Lerp(
                Mathf.Lerp(d000, d100, u),
                Mathf.Lerp(d010, d110, u),
                v
            ),
            Mathf.Lerp(
                Mathf.Lerp(d001, d011, u),
                Mathf.Lerp(d101, d111, u),
                v
            ),
            w);
    }
    
    public static float Perlin2D(Vector2 point, float frequency)
    {
        Profiler.BeginSample("Perlin2D");
        Init();

        point *= frequency;
        // same thing, with one less coordinate!
        int xi = Mathf.FloorToInt(point.x);
        int yi = Mathf.FloorToInt(point.y);
        
        float xf = point.x - xi;
        float yf = point.y - yi;
        
        // We now have unit squares!
        xi &= 255;
        yi &= 255;

        // Compute the fade (smoothing) curves for each mapped coordinate
        float u = Fade(xf);
        float v = Fade(yf);

        // Get the right gradient for each vertex of the square
        Vector2 g00 = _gradients2D[Permutation2X[Permutation2X[xi] + yi] & 7];
        Vector2 g10 = _gradients2D[Permutation2X[Permutation2X[(xi + 1)] + yi] & 7];
        Vector2 g01 = _gradients2D[Permutation2X[Permutation2X[xi] + (yi + 1)] & 7];
        Vector2 g11 = _gradients2D[Permutation2X[Permutation2X[(xi + 1)] + (yi + 1)] & 7];

        // Same thing but simpler
        float d00 = Vector2.Dot(g00, new Vector2(xf, yf));
        float d10 = Vector2.Dot(g10, new Vector3(xf - 1, yf));
        float d01 = Vector2.Dot(g01, new Vector3(xf, yf - 1));
        float d11 = Vector2.Dot(g11, new Vector3(xf - 1, yf - 1));
        
        /*
         * Again, same but less Lerping :D
         * Why sqr2? Because we the maximum value we can get is by interpolating 4 diagonal gradients pointing towards
         * the center, and that value is sqrt(1/2) (in 1D it would be two opposing gradients with a value of 0.5, but
         * we are working in 2D now, so let's square that), so we multiply by sqrt(2) to normalize.
         * We didn't need to normalize the 3D noise because all the gradients use exactly 2 dimensions, so the maximum
         * value is always 1!
         */
        Profiler.EndSample();
        return Mathf.Lerp(
                Mathf.Lerp(d00, d10, u),
                Mathf.Lerp(d01, d11, u),
                v
            );
    }
    
    /*
     * Improvement over the original algorithm: the cubic interpolant function (3t^2 + 2t^3) is replaced with 
     * a quintic interpolant function 6t^5 - 15t^4 + 10t^3, which has zero 1st and 2nd derivatives at t = 0  and t = 1
     * thus preventing the forming of artifacts caused by discontinuities along coordinate-aligned faces of adjoining
     * cubic cells (for humans like me, it avoids artifacts where two adjacent unit cubes have co-planar faces).
     * */
    private static float Fade(float t)  { return t * t * t * (t * (t * 6 - 15) + 10); }
    
    /*
     * Improvement over the original algorithm: the original algorithm uses a complicated function to generate
     * pseudorandom gradient vectors on each unit coordinate and calculate their dot products. The vectors are uniformly
     * distributed over a sphere, leading to directional biases in the grid along axes (shorter) and diagonals (longer)
     * (again, for humans, it avoids artifacts where close gradients would align and produce weird clumps).
     * Ken Perlin reasons that, since the permutation P already provides enough randomness, there is no need for
     * additional randomization, and replaces the pseudorandom vectors with 12 vectors defined by the direction from the
     * centre of a cube to its edges, chosen by the result of the hash value from P modulo 12. The set of gradients was
     * chosen to avoid artifacts caused by directional biases. It is also more efficient in terms of operations!
     */
    private static Vector3[] _gradients3D = {
        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 1f,-1f, 0f),
        new Vector3(-1f,-1f, 0f),
        new Vector3( 1f, 0f, 1f),
        new Vector3(-1f, 0f, 1f),
        new Vector3( 1f, 0f,-1f),
        new Vector3(-1f, 0f,-1f),
        new Vector3( 0f, 1f, 1f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f, 1f,-1f),
        new Vector3( 0f,-1f,-1f),
        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f,-1f,-1f)
    };

    // Adapted version to make 2D noise, I use axes directions and diagonals instead. Still no need for more randomness!
    // Bitwise masking to take the value of the last 3 bits of hash and map them to the 8 gradients

    private static Vector2[] _gradients2D = {
        new Vector2( 1f, 0f),
        new Vector2(-1f, 0f),
        new Vector2( 0f, 1f),
        new Vector2( 0f,-1f),
        new Vector2( 1f, 1f).normalized,
        new Vector2(-1f, 1f).normalized,
        new Vector2( 1f,-1f).normalized,
        new Vector2(-1f,-1f).normalized
    };
}
