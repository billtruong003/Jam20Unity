Shader "URP/Unlit/FlagWindNaturalTopLock_UV_InstancedQuest2_Fixed"
{
    Properties
    {
        _BaseMap ("Texture Array", 2DArray) = "" {}
        _BaseColor ("Color", Color) = (1,1,1,1)

        _WaveSpeed ("Wind Speed", Float) = 1.0
        _WaveAmplitude ("Wind Amplitude", Float) = 0.05

        _TopHoldStrength ("Top Hold Strength", Range(0,1)) = 1.0

        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseStrength ("Noise Strength", Float) = 0.3

        _WindDirection ("Wind Direction (XYZ)", Vector) = (1, 0, 0.2, 0)

        // UV-based locking
        _TopFadeV ("Top Fade (UV 0..1)", Range(0,1)) = 0.25
        _HolderVThickness ("Top Holder Thickness (UV 0..1)", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "UnlitFlagWind"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID    // <- cần cho instancing
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half    fade : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID    // <- mang Instance ID sang frag
            };

            TEXTURE2D_ARRAY(_BaseMap);
            SAMPLER(sampler_BaseMap);

            half4 _BaseColor;
            half _WaveSpeed;
            half _WaveAmplitude;
            half _TopHoldStrength;
            half _NoiseScale;
            half _NoiseStrength;
            half4 _WindDirection;
            half _TopFadeV;
            half _HolderVThickness;

            // Per-instance property: _LayerIndex
            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _LayerIndex)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            // Simple pseudo-random
            half rand(half2 co)
            {
                return frac(sin(dot(co.xy, half2(12.9898h, 78.233h))) * 43758.5453h);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);  // <- QUAN TRỌNG

                half3 pos = IN.positionOS.xyz;
                half2 uv  = IN.uv;

                half time = _Time.y * _WaveSpeed;
                half3 windDir = normalize(_WindDirection.xyz);

                // Fade theo UV (v=1 là top)
                half holderVStart = 1.0h - _HolderVThickness;
                half fadeZone = saturate((holderVStart - uv.y) / _TopFadeV);
                half fade = pow(fadeZone, 2.5h) * step(uv.y, holderVStart - 1e-4h);

                if (fade > 0.001h)
                {
                    half windPulse = sin(time * 0.6h) * 0.5h + 0.5h;
                    half gust = sin(time * 1.5h + pos.x * 0.5h) * 0.3h;
                    half windStrength = saturate(windPulse + gust * 0.5h);

                    half randomOffset = rand(pos.xz * _NoiseScale);
                    half flutter = sin(pos.z * 3.0h + time * 2.0h + randomOffset * 6.2831h) * _NoiseStrength;

                    half waveDisplacement = _WaveAmplitude * windStrength * fade * _TopHoldStrength;
                    pos += windDir * waveDisplacement;
                    pos.y += flutter * 0.05h * fade;
                    uv.x += fade * 0.1h;
                }

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = uv;
                OUT.fade = fade;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                UNITY_SETUP_INSTANCE_ID(IN);  // <- QUAN TRỌNG: set instance cho frag

                // Lấy layer theo instance
                half layer = floor(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _LayerIndex) + 0.5h);

                half2 uv = saturate(IN.uv);
                half4 texColor = SAMPLE_TEXTURE2D_ARRAY(_BaseMap, sampler_BaseMap, uv, layer);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}