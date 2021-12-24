Shader "Perlin FBm"
{
    Properties
    {
        multiplier ("Multiplier", Float) = 1.0
        offset_x ("OffsetX",Float) = 0.0
        offset_z ("OffsetZ",Float) = 0.0     
        [PerRendererData] local_offset_x ("LocalOffsetX",Float) = 0.0
        [PerRendererData] local_offset_z ("LocalOffsetZ",Float) = 0.0      
        octaves ("Octaves",Int) = 1
        lacunarity("Lacunarity", Range( 1.0 , 5.0)) = 2
        gain("Gain", Range( 0.0 , 1.0)) = 0.5
        amplitude("Amplitude", Range( 0.0 , 5.0)) = 1.5
        frequency("Frequency", Range( 0.0 , 16.0)) = 2.0
        exponent("Exponent", Range( 0.1 , 5.0)) = 1.0
        scale("Scale", Float) = 1.0
        zcoord("Z Coord", Float) = 0.0
        wcoord("W Coord", Float) = 0.0
    }
    SubShader
    {
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include <UnityShaderUtilities.cginc>

            #include "NoiseFunctions.cginc"
            struct shader_data
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            

            float multiplier, octaves, lacunarity, gain, value, amplitude, frequency, offset_x, offset_z, local_offset_x, local_offset_z, scale, exponent, zcoord, wcoord;

            shader_data vert(float4 vertex:POSITION, float2 uv:TEXCOORD0)
            {
                shader_data vs;
                vs.vertex = UnityObjectToClipPos (vertex);
                vs.uv = uv;
                return vs;
            }

            float4 frag(shader_data ps) : SV_Target
            {
                float2 input = ps.uv.xy;
                input += float2(offset_x, offset_z);
                input -= float2(0.5, 0.5);
                const fbm_settings settings = make_fbm_settings(multiplier, octaves, lacunarity, gain, amplitude, frequency, scale, exponent);
                // simplex
                // float c = fbm_noise::gen(simplex_noise::gen(), settings).compute(float4(input, zcoord, wcoord));
                // perlin
                // float c = fbm_noise::gen(perlin_noise::gen(), settings).compute(float4(input, zcoord, wcoord));
                // cool marble
                float c = fbm_noise::gen(abs_noise::gen(marble_noise::gen(simplex_noise::gen(), 0.3)), settings).compute(float4(input, zcoord, wcoord));
                return float4(c,c,c,1);
            }
            ENDHLSL
        }
    }
}
