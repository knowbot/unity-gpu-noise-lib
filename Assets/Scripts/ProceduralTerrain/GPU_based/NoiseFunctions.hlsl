// ReSharper disable CppLocalVariableMayBeConst
// ReSharper disable CppParameterMayBeConst
#pragma target 4.0

#ifndef NOISE_FUNCTIONS
#define NOISE_FUNCTIONS

#define PI 3.141592653589793238462
#define TWO_PI PI * 2
#define HALF_PI PI / 2

// forward declarations
float perlin(float2 input);
float perlin(float3 input);
float perlin(float4 input);
float simplex(float2 input);
float simplex(float3 input);
float simplex(float4 input);

// helper struct
struct fbm_settings
{
    float multiplier, octaves, lacunarity, gain, amplitude, frequency, scale, exponent;
};

fbm_settings make_fbm_settings(float multiplier, float octaves, float lacunarity, float gain, float amplitude,
    float frequency, float scale, float exponent)
{
    fbm_settings settings;
    settings.multiplier = multiplier;
    settings.octaves = octaves;
    settings.lacunarity = lacunarity;
    settings.gain = gain;
    settings.amplitude = amplitude;
    settings.frequency = frequency;
    settings.scale = scale;
    settings.exponent = exponent;
    return settings;
}

/*
 * The following implementation to allow easy concatenation of noise functions was implemented following the absolutely
 * bonkers idea found here: http://code4k.blogspot.com/2011/11/advanced-hlsl-using-closures-and.html
 * This guy is THE madlad and I love this implementation, even though Rider apparently does not.
 */

interface i_noise {
    i_noise next();
    float compute(float2 p);
    float compute(float3 p);
    float compute(float4 p);
};

class noise_base : i_noise {
    i_noise next()
    {
        noise_base base;
        return base;
    }

    float compute(float2 p)
    {
        return next().compute(p);
    }

    float compute(float3 p)
    {
        return next().compute(p);
    }

    float compute(float4 p)
    {
        return next().compute(p);
    }
};

class perlin_noise : noise_base {
    
    static i_noise gen() {
        perlin_noise noise;
        return noise;
    }

    float compute(float2 p) {
        return perlin(p);
    }

    float compute(float3 p) {
        return perlin(p);
    }

    float compute(float4 p) {
        return perlin(p);
    }
};

class simplex_noise : noise_base {
    
    static i_noise gen() {
        simplex_noise noise;
        return noise;
    }

    float compute(float2 p) {
        return simplex(p);
    }

    float compute(float3 p) {
        return simplex(p);
    }

    float compute(float4 p) {
        return simplex(p);
    }
};

class abs_noise : noise_base {
    static i_noise gen(i_noise from)
    {
        class local_noise : abs_noise { i_noise next() { return from; } } noise;
        return noise;
    }

    float compute(float2 p)
    {
        return abs(next().compute(p));
    }

    float compute(float3 p)
    {
        return abs(next().compute(p));
    }

    float compute(float4 p)
    {
        return abs(next().compute(p));
    }
};

class invert_noise : noise_base {
    static i_noise gen(i_noise from)
    {
        class local_noise : invert_noise { i_noise next() { return from; } } noise;
        return noise;
    }

    float compute(float2 p)
    {
        return -1.0 *  next().compute(p);
    }

    float compute(float3 p)
    {
        return -1.0 *  next().compute(p);
    }

    float compute(float4 p)
    {
        return -1.0 *  next().compute(p);
    }
};

class marble_noise : noise_base {

    float perturbation;
    
    static i_noise gen(i_noise from, float perturbation)
    {
        class local_noise : marble_noise { i_noise next() { return from; } } noise;
        noise.perturbation = perturbation;
        return noise;
    }

    float stripes(float x) {
        float t = .5 + .5 * sin(perturbation * 2 * PI * x);
        return t * t - .5;
    }

    float compute(float2 p)
    {
        return stripes(p.x + 2 * next().compute(p));
    }


    float compute(float3 p)
    {
        return stripes(p.x + 2 * next().compute(p));
    }

    float compute(float4 p)
    {
        return stripes(p.x + 2 * next().compute(p));
    }
};


class fbm_noise : noise_base {
    fbm_settings settings;

    static i_noise gen(i_noise from, fbm_settings settings) {
        class local_noise : fbm_noise { i_noise next() { return from; } } noise;
        noise.settings = settings;
        return noise;
    }

    float compute(float2 p) {
        float range = 1.f;
        p *= settings.scale;
        float freq = settings.frequency;
        float amp = settings.amplitude;
        float value = amp * next().compute(p * freq);  
        for(int i = 1; i < settings.octaves; i++)
        {
            freq *= settings.lacunarity;
            amp *= settings.gain;
            range += amp;
            value += amp * next().compute(p * freq);  
        }
        // scales back to [-1.0, 1.0] interval
        value /= range;
        // make super sure that value is in the expected range
        value = clamp(value, -1.0, 1.0);
        // normalize it to [0, 1]
        value = (value + 1) / 2;
        return pow(value, settings.exponent) * settings.multiplier;
    }

    float compute(float3 p) {
        float range = 1.f;
        p *= settings.scale;
        float freq = settings.frequency;
        float amp = settings.amplitude;
        float value = amp * next().compute(p * freq);  
        for(int i = 1; i < settings.octaves; i++)
        {
            freq *= settings.lacunarity;
            amp *= settings.gain;
            range += amp;
            value += amp * next().compute(p * freq);  
        }
        // scales back to [-1.0, 1.0] interval
        value /= range;
        // make super sure that value is in the expected range
        value = clamp(value, -1.0, 1.0);
        // normalize it to [0, 1]
        value = (value + 1) / 2;
        return pow(value, settings.exponent) * settings.multiplier;
    }

    float compute(float4 p) {
        float range = 1.f;
        p *= settings.scale;
        float freq = settings.frequency;
        float amp = settings.amplitude;
        float value = amp * next().compute(p * freq);  
        for(int i = 1; i < settings.octaves; i++)
        {
            freq *= settings.lacunarity;
            amp *= settings.gain;
            range += amp;
            value += amp * next().compute(p * freq);  
        }
        // scales back to [-1.0, 1.0] interval
        value /= range;
        // make super sure that value is in the expected range
        value = clamp(value, -1.0, 1.0);
        // normalize it to [0, 1]
        value = (value + 1) / 2;
        return pow(value, settings.exponent) * settings.multiplier;
    }
};



// CONSTANTS
// Credit to: https://www.researchgate.net/publication/216813608_Simplex_noise_demystified
static const float sqr2 = sqrt(2.0);
static const float sqr5 = sqrt(5.0);
static const float f2 = 0.5 * (sqrt(3.0) - 1.0); // 2d simplex skew constant
static const float g2 = (3.0 - sqrt(3.0)) / 6.0; // 2d simplex unskew constant
static const float f3 = 1.0 / 3.0; // 3d simplex skew constant
static const float g3 = 1.0 / 6.0; // 3d simplex unskew constant
static const float f4 = (sqr5 - 1.0) / 4.0; // 4d simplex skew constant
static const float g4 = (5.0 - sqr5) / 20.0; // 4d simplex unskew constant

// PERMUTATION TABLES
static const int perm[512] = {
        151,160,137,91,90,15,
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
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
        151,160,137,91,90,15,
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

// GRADIENT TABLES
static const float2 gradients_2d[8] = {
    float2(1, 0),
    float2(-1, 0),
    float2(0, 1),
    float2(0, -1),
    normalize(float2(1, 1)),
    normalize(float2(-1, 1)),
    normalize(float2(1, -1)),
    normalize(float2(-1, -1))
};

static const float3 gradients_3d[16] = {
    float3( 1,  1,  0),
    float3(-1,  1,  0),
    float3( 1, -1,  0),
    float3(-1, -1,  0),
    float3( 1,  0,  1),
    float3(-1,  0,  1),
    float3( 1,  0, -1),
    float3(-1,  0, -1),
    float3( 0,  1,  1),
    float3( 0, -1,  1),
    float3( 0,  1, -1),
    float3( 0, -1, -1),
    float3( 1,  1,  0),
    float3(-1,  1,  0),
    float3( 0, -1,  1),
    float3( 0, -1, -1)
};


// This is slightly more efficient for 3D noise as it removes the need to compute dot products.
static const float grad3d(int hash, float3 p)
{
    switch (hash & 0xF)
    {
        case 0x0: return   p.x + p.y;
        case 0x1: return - p.x + p.y;
        case 0x2: return   p.x - p.y;
        case 0x3: return - p.x - p.y;
        case 0x4: return   p.x + p.z;
        case 0x5: return - p.x + p.z;
        case 0x6: return   p.x - p.z;
        case 0x7: return - p.x - p.z;
        case 0x8: return   p.y + p.z;
        case 0x9: return - p.y + p.z;
        case 0xA: return   p.y - p.z;
        case 0xB: return - p.y - p.z;
        case 0xC: return   p.y + p.x;
        case 0xD: return - p.y + p.z;
        case 0xE: return   p.y - p.x;
        case 0xF: return - p.y - p.z;
        default: return 0;
    }
}

// This is slightly more efficient for 4D noise as it removes the need to compute dot products.
static const float grad4d(int hash, float4 p)
{
    switch (hash & 0x1F)
    {
        case 0x00: return   p.y + p.z + p.w;
        case 0x01: return   p.y + p.z - p.w;
        case 0x02: return   p.y - p.z + p.w;
        case 0x03: return   p.y - p.z - p.w;
        case 0x04: return - p.y + p.z + p.w;
        case 0x05: return - p.y + p.z - p.w;
        case 0x06: return - p.y - p.z + p.w;
        case 0x07: return - p.y - p.z - p.w;
        case 0x08: return   p.x + p.z + p.w;
        case 0x09: return   p.x + p.z - p.w;
        case 0x0A: return   p.x - p.z + p.w;
        case 0x0B: return   p.x - p.z - p.w;
        case 0x0C: return - p.x + p.z + p.w;
        case 0x0D: return - p.x + p.z - p.w;
        case 0x0E: return - p.x - p.z + p.w;
        case 0x0F: return - p.x - p.z - p.w;
        case 0x10: return   p.x + p.y + p.w;
        case 0x11: return   p.x + p.y - p.w;
        case 0x12: return   p.x - p.y + p.w;
        case 0x13: return   p.x - p.y - p.w;
        case 0x14: return - p.x + p.y + p.w;
        case 0x15: return - p.x + p.y - p.w;
        case 0x16: return - p.x - p.y + p.w;
        case 0x17: return - p.x - p.y - p.w;
        case 0x18: return   p.x + p.y + p.z;
        case 0x19: return   p.x + p.y - p.z;
        case 0x1A: return   p.x - p.y + p.z;
        case 0x1B: return   p.x - p.y - p.z;
        case 0x1C: return - p.x + p.y + p.z;
        case 0x1D: return - p.x + p.y - p.z;
        case 0x1E: return - p.x - p.y + p.z;
        case 0x1F: return - p.x - p.y - p.z;
        default: return 0;
    }
}

// HELPER FUNCTIONS

static float smooth(float t){ return t * t * t * (t * (t * 6 - 15) + 10); }
static float lerp(float a, float b, float t) { return a + (b -a ) * t; }
static int fast_floor(float x) { return x > 0 ? (int)x : (int)x - 1; }


// CLASSIC PERLIN FUNCTIONS
float perlin(float2 input)
{
    int2 o = int2(
       fast_floor(input.x),
       fast_floor(input.y)
    );

    float2 f = float2(
        input.x - o.x,
        input.y - o.y
    );

    o.x &= 255;
    o.y &= 255;

    float u = smooth(f.x);
    float v = smooth(f.y);

    // Get the right gradient for each vertex of the square
    float2 g00 = gradients_2d[perm[perm[o.x    ] + o.y    ] & 7];
    float2 g10 = gradients_2d[perm[perm[o.x + 1] + o.y    ] & 7];
    float2 g01 = gradients_2d[perm[perm[o.x    ] + o.y + 1] & 7];
    float2 g11 = gradients_2d[perm[perm[o.x + 1] + o.y + 1] & 7];

    // Same thing but simpler
    float d00 = dot(g00, float2(f.x    , f.y    ));
    float d10 = dot(g10, float2(f.x - 1, f.y    ));
    float d01 = dot(g01, float2(f.x    , f.y - 1));
    float d11 = dot(g11, float2(f.x - 1, f.y - 1));

    return lerp(
            lerp(d00, d10, u),
            lerp(d01, d11, u),
            v
        ) * sqr2;
}

// Given a float3 point, returns a coherent noise value between -1 and 1
float perlin(float3 input)
{
    int3 o = int3(
        fast_floor(input.x),
        fast_floor(input.y),
        fast_floor(input.z)
    );
    
    float3 f = float3 (
        input.x - o.x,
        input.y - o.y,
        input.z - o.z
    );

    o.x &= 255;
    o.y &= 255;
    o.z &= 255;

    float u = smooth(f.x);
    float v = smooth(f.y);
    float s = smooth(f.z);


    // Slightly different implementation from 2D, more efficient cause it has less multiplications.
    int g000 = perm[perm[perm[o.x    ] + o.y    ] + o.z    ];
    int g100 = perm[perm[perm[o.x + 1] + o.y    ] + o.z    ];
    int g010 = perm[perm[perm[o.x    ] + o.y + 1] + o.z    ];
    int g110 = perm[perm[perm[o.x + 1] + o.y + 1] + o.z    ];
    int g001 = perm[perm[perm[o.x    ] + o.y    ] + o.z + 1];
    int g101 = perm[perm[perm[o.x + 1] + o.y    ] + o.z + 1];
    int g011 = perm[perm[perm[o.x    ] + o.y + 1] + o.z + 1];
    int g111 = perm[perm[perm[o.x + 1] + o.y + 1] + o.z + 1];

    float d000 = grad3d(g000, float3(f.x    , f.y    , f.z    ));
    float d100 = grad3d(g100, float3(f.x - 1, f.y    , f.z    ));
    float d010 = grad3d(g010, float3(f.x    , f.y - 1, f.z    ));
    float d110 = grad3d(g110, float3(f.x - 1, f.y - 1, f.z    ));
    float d001 = grad3d(g001, float3(f.x    , f.y    , f.z - 1));
    float d101 = grad3d(g101, float3(f.x - 1, f.y    , f.z - 1));
    float d011 = grad3d(g011, float3(f.x    , f.y - 1, f.z - 1));
    float d111 = grad3d(g111, float3(f.x - 1, f.y - 1, f.z - 1));
    return lerp(
            lerp(
                lerp(d000, d100, u),
                lerp(d010, d110, u),
                v
            ),
            lerp(
                lerp(d001, d101, u),
                lerp(d011, d111, u),
                v
            ),
        s);
}

float perlin(float4 input)
{
    int4 o = int4(
        fast_floor(input.x),
        fast_floor(input.y),
        fast_floor(input.z),
        fast_floor(input.w)
    );

    float4 f = float4(
        input.x - o.x,
        input.y - o.y,
        input.z - o.z,
        input.w - o.w
    );


    o.x &= 255;
    o.y &= 255;
    o.z &= 255;
    o.w &= 255;

    float u = smooth(f.x);
    float v = smooth(f.y);
    float s = smooth(f.z);
    float t = smooth(f.w);

    // Slightly different implementation from 2D, more efficient cause it has less multiplications.
    int g0000 = perm[perm[perm[perm[o.x    ] + o.y    ] + o.z    ] + o.w    ];
    int g1000 = perm[perm[perm[perm[o.x + 1] + o.y    ] + o.z    ] + o.w    ];
    int g0100 = perm[perm[perm[perm[o.x    ] + o.y + 1] + o.z    ] + o.w    ];
    int g1100 = perm[perm[perm[perm[o.x + 1] + o.y + 1] + o.z    ] + o.w    ];
    int g0010 = perm[perm[perm[perm[o.x    ] + o.y    ] + o.z + 1] + o.w    ];
    int g1010 = perm[perm[perm[perm[o.x + 1] + o.y    ] + o.z + 1] + o.w    ];
    int g0110 = perm[perm[perm[perm[o.x    ] + o.y + 1] + o.z + 1] + o.w    ];
    int g1110 = perm[perm[perm[perm[o.x + 1] + o.y + 1] + o.z + 1] + o.w    ];
    int g0001 = perm[perm[perm[perm[o.x    ] + o.y    ] + o.z    ] + o.w + 1];
    int g1001 = perm[perm[perm[perm[o.x + 1] + o.y    ] + o.z    ] + o.w + 1];
    int g0101 = perm[perm[perm[perm[o.x    ] + o.y + 1] + o.z    ] + o.w + 1];
    int g1101 = perm[perm[perm[perm[o.x + 1] + o.y + 1] + o.z    ] + o.w + 1];
    int g0011 = perm[perm[perm[perm[o.x    ] + o.y    ] + o.z + 1] + o.w + 1];
    int g1011 = perm[perm[perm[perm[o.x + 1] + o.y    ] + o.z + 1] + o.w + 1];
    int g0111 = perm[perm[perm[perm[o.x    ] + o.y + 1] + o.z + 1] + o.w + 1];
    int g1111 = perm[perm[perm[perm[o.x + 1] + o.y + 1] + o.z + 1] + o.w + 1];

    float d0000 = grad4d(g0000, float4(f.x    , f.y    , f.z    , f.w    ));
    float d1000 = grad4d(g1000, float4(f.x - 1, f.y    , f.z    , f.w    ));
    float d0100 = grad4d(g0100, float4(f.x    , f.y - 1, f.z    , f.w    ));
    float d1100 = grad4d(g1100, float4(f.x - 1, f.y - 1, f.z    , f.w    ));
    float d0010 = grad4d(g0010, float4(f.x    , f.y    , f.z - 1, f.w    ));
    float d1010 = grad4d(g1010, float4(f.x - 1, f.y    , f.z - 1, f.w    ));
    float d0110 = grad4d(g0110, float4(f.x    , f.y - 1, f.z - 1, f.w    ));
    float d1110 = grad4d(g1110, float4(f.x - 1, f.y - 1, f.z - 1, f.w    ));
    float d0001 = grad4d(g0001, float4(f.x    , f.y    , f.z    , f.w - 1));
    float d1001 = grad4d(g1001, float4(f.x - 1, f.y    , f.z    , f.w - 1));
    float d0101 = grad4d(g0101, float4(f.x    , f.y - 1, f.z    , f.w - 1));
    float d1101 = grad4d(g1101, float4(f.x - 1, f.y - 1, f.z    , f.w - 1));
    float d0011 = grad4d(g0011, float4(f.x    , f.y    , f.z - 1, f.w - 1));
    float d1011 = grad4d(g1011, float4(f.x - 1, f.y    , f.z - 1, f.w - 1));
    float d0111 = grad4d(g0111, float4(f.x    , f.y - 1, f.z - 1, f.w - 1));
    float d1111 = grad4d(g1111, float4(f.x - 1, f.y - 1, f.z - 1, f.w - 1));
    return lerp (
        lerp(
            lerp(
                lerp(d0000, d1000, u),
                lerp(d0100, d1100, u),
                v
            ),
            lerp(
                lerp(d0010, d1010, u),
                lerp(d0110, d1110, u),
                v
            ), 
            s),
        lerp(
            lerp(
                lerp(d0001, d1001, u),
                lerp(d0101, d1101, u),
                v
            ),
            lerp(
                lerp(d0011, d1011, u),
                lerp(d0111, d1111, u),
                v
            ), 
            s),
        t);
}

// SIMPLEX FUNCTIONS
float simplex(float2 input)
{
    // clarification: (0,0) is common to the two triangles, middle corner is different, (1,1) is common
    float c[3]; // contributions from each corner
    const float skew = (input.x + input.y) * f2; // skew factor
    // cell origin coords in skewed space (squared grid)
    int2 o = int2(
        fast_floor(input.x + skew),
        fast_floor(input.y + skew)
    );
    
    float unskew = (o.x + o.y) * g2; // unskew factor

    // coordinates of corners in unskewed space
    float2 p[3];
    // origin, in unskewed space
    p[0] = float2(
        input.x - o.x + unskew,
        input.y - o.y + unskew
    );
    
    // we are in 2D, so determine in which triangle (simplex) we are
    int2 offs; // offsets from middle corner in skewed coordinates
    if(p[0].x >= p[0].y)
        // we are in the lower triangle (0,0) > (1,0) > (1,1)
        offs = int2(1 ,0);
    else
        // we are in the upper triangle (0,0) > (0,1) > (1,1)
        offs = int2(0, 1);
    
    // coords of middle corner in unskewed space
    p[1] = float2(
        p[0].x - offs.x + g2,
        p[0].y - offs.y + g2
    );
    
    // coords of end corner in unskewed space
    p[2] = float2(
        p[0].x - 1.0 + 2.0 * g2,
        p[0].y - 1.0 + 2.0 * g2
    );
    
    // get hashed gradients (in skewed space)
    int ixs = o.x & 255;
    int iys = o.y & 255;

    // gradients
    float2 gs[3];
    gs[0] = gradients_2d[perm[ixs +          perm[iys         ]] & 7];
    gs[1] = gradients_2d[perm[ixs + offs.x + perm[iys + offs.y]] & 7];
    gs[2] = gradients_2d[perm[ixs +      1 + perm[iys +      1]] & 7];
    // get contribution from each corner
    for(int i = 0; i < 3; i++) {
        float t = 0.5 - p[i].x * p[i].x - p[i].y * p[i].y;
        if(t < 0.0)
            c[i] = 0.0;
        else
        {
            t *= t;
            c[i] = t * t * dot(gs[i], p[i]);
        }
    }    

    // scale back to [-1,1] interval (no clue why these exact values in this case)
    return 70.0f * (c[0] + c[1] + c[2]);
}

float simplex(float3 input)
{
    float c[4]; // now we have 4 corners! cause the simplex is a tetrahedron now
    float skew = (input.x + input.y + input.z) * f3; // skew factor
 
    // cell origin coords in skewed space (squared grid)
    int3 o = int3(
        fast_floor(input.x + skew),
        fast_floor(input.y + skew),
        fast_floor(input.z + skew)
        );
    
    float unskew = (o.x + o.y + o.z) * g3; // unskew factor

    // corner positions in unskewed space
    float3 p[4];
    // origin in unskewed space
    p[0] = float3(
        input.x - o.x + unskew,
        input.y - o.y + unskew,
        input.z - o.z + unskew
    );
    
    // to determine in which tetrahedron (simplex) we are, we need offsets for corners
    int3 offs[4];
    // origin has no offset
    offs[0] = int3(0, 0, 0);
    if(p[0].x >= p[0].y)
    {
        if(p[0].y >= p[0].z)
        {   // XYZ order
            offs[1] = int3(1, 0, 0);
            offs[2] = int3(1, 1, 0);
        } else if(p[0].x >= p[0].z)
        {   // XZY order
            offs[1] = int3(1, 0, 0);
            offs[2] = int3(1, 0, 1);
        } else
        {   // ZXY order
            offs[1] = int3(0, 0, 1);
            offs[2] = int3(1, 0, 1);
        }
    } else {
        if(p[0].y < p[0].z)
        {   // ZYX order
            offs[1] = int3(0, 0, 1);
            offs[2] = int3(0, 1, 1);
        } else if(p[0].x < p[0].z)
        {   // YZX order
            offs[1] = int3(0, 1, 0);
            offs[2] = int3(0, 1, 1);
        } else
        {   // YXZ order
            offs[1] = int3(0, 1, 0);
            offs[2] = int3(1, 1, 0);
        }
        // offset for the last corner is always known
        offs[3] = int3(1, 1, 1);
    }
    int i;
    for(i = 1; i < 4; i++) {
        float f = g3 * i;
        p[i] = p[0] - offs[i] + float3(f, f, f);
    }

    // get hashed gradients (in skewed space)
    int ixs = o.x & 255;
    int iys = o.y & 255;
    int izs = o.z & 255;
    
    // calc indexes
    int h[4];
    for(i = 0; i < 4; i++)
        h[i] = perm[ixs + offs[i].x + perm[iys + offs[i].y + perm[izs + offs[i].z]]];

    // get contribution from each corner
     // get contribution from each corner
    for(i = 0; i < 4; i++) {
        float t = 0.5 - p[i].x * p[i].x - p[i].y * p[i].y - p[i].z * p[i].z;
        if(t < 0.0)
            c[i] = 0.0;
        else
        {
            t *= t;
            c[i] = t * t * grad3d(h[i], p[i]);
        }
    }
    //
    // scale back to [-1,1] interval (no clue why these exact values)
    return 32.0 * (c[0] + c[1] + c[2] + c[3]);
}

float simplex(float4 input) {
    // in 4 dimensions, the simplex is a 5-cell, aka a 4D object bound by 5 tetrahedral cells. see https://en.wikipedia.org/wiki/5-cell
    float c[5]; // now we have 5 corners!
    float skew = (input.x + input.y + input.z + input.w) * f4; // skew factor
 
    // cell origin coords in skewed space (squared grid)
    int4 o = int4(
        fast_floor(input.x + skew),
        fast_floor(input.y + skew),
        fast_floor(input.z + skew),
        fast_floor(input.w + skew)
    );
    
    float unskew = (o.x + o.y + o.z + o.w) * g4; // unskew factor
    // distances from cell origin, in unskewed space
    float4 p[5];
    p[0] = float4(
        input.x - o.x + unskew,
        input.y - o.y + unskew,
        input.z - o.z + unskew,
        input.w - o.w + unskew
    );
    
    // we are in 4D, so determine in which 5-cell (simplex) we are out of the 24 simplices that make up the hypercube in skewed space
    // the shape is however way more complex and the pair-wise comparisons of x,y,z,w have some impossible edge cases (e.g. x > y > w > z makes w > x impossible)
    // the 4 coordinates have 6 possible unordered pairs. to represent each pairwise comparison, we use binary bits coded into an integer index
    int4 order = int4(0, 0, 0, 0);
    order += (p[0].x > p[0].y) ? int4(1, 0, 0, 0) : int4(0, 1, 0, 0);
    order += (p[0].x > p[0].z) ? int4(1, 0, 0, 0) : int4(0, 0, 1, 0);
    order += (p[0].x > p[0].w) ? int4(1, 0, 0, 0) : int4(0, 0, 0, 1);
    order += (p[0].y > p[0].z) ? int4(0, 1, 0, 0) : int4(0, 0, 1, 0);
    order += (p[0].y > p[0].w) ? int4(0, 1, 0, 0) : int4(0, 0, 0, 1);
    order += (p[0].z > p[0].w) ? int4(0, 0, 1, 0) : int4(0, 0, 0, 1);

    if(order.x + order.y + order.z + order.w == 12)
        return 1;

    int4 offs[5];
    offs[0] = int4(0, 0, 0, 0);

    int i;
    for(i = 1; i < 4; i++) {
        offs[i].x = order.x >= (4 - i) ? 1 : 0;
        offs[i].y = order.y >= (4 - i) ? 1 : 0;
        offs[i].z = order.z >= (4 - i) ? 1 : 0;
        offs[i].w = order.w >= (4 - i) ? 1 : 0;
    }
    
    // we know that 5th corner is (1,1,1,1)
    offs[4] = int4(1, 1, 1, 1);

    // now, we calculate the coordinates of each corner in unskewed space
    for(i = 1; i < 5; i++) {
        float f = g4 * i;
        p[i] = p[0] - offs[i] + float4(f, f, f, f);
    }


    // get hashed gradients (in skewed space)
    int ixs = o.x & 255;
    int iys = o.y & 255;
    int izs = o.z & 255;
    int iws = o.w & 255;
    
    // calc indexes
    int h[5];
    for(i = 0; i < 5; i++)
        h[i] = perm[ixs + offs[i].x + perm[iys + offs[i].y + perm[izs + offs[i].z + perm[iws +  offs[i].w]]]];
    
    // get contribution from each corner
    for(i = 0; i < 5; i++) {
        float t = 0.5 - p[i].x * p[i].x - p[i].y * p[i].y - p[i].z * p[i].z - p[i].w * p[i].w;;
        if(t < 0.0)
            c[i] = 0.0;
        else
        {
            t *= t;
            c[i] = t * t * grad4d(h[i], p[i]);
        }
    }

    // scale back to [-1,1] interval (no clue why these exact values)
    return 27.0 * (c[0] + c[1] + c[2] + c[3] + c[4]);
}
#endif