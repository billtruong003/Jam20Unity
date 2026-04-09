Shader "BillTheDev/URP_Aura_Morph"
{
    Properties
    {
        [Header(Shape and Flow)]
        _OutlineWidth("Base Width", Range(0, 0.5)) = 0.05
        _DisplacementStrength("Wave Strength (Morph)", Range(0, 0.2)) = 0.05
        _VerticalRise("Vertical Flow", Range(0, 0.5)) = 0.1
        _ZOffset("Z Offset (Camera Push)", Range(-1, 1)) = -0.05

        [Header(Colors)]
        [HDR] _CoreColor("Core Color", Color) = (1, 0.8, 0, 1)
        [HDR] _RimColor("Rim Color", Color) = (1, 0.2, 0, 1)

        [Header(Noise Texture)]
        _NoiseTex("Noise Texture (Seamless)", 2D) = "white" {}
        _NoiseScale("Noise Tiling", Float) = 1.5
        _FlowSpeedX("Flow Speed X", Float) = 0.0
        _FlowSpeedY("Flow Speed Y", Float) = 1.0

        [Header(Rim Effect)]
        _RimPower("Rim Power (Fill)", Range(0.1, 8.0)) = 2.0
        _RimSharpness("Edge Sharpness", Range(1.0, 20.0)) = 5.0
        _CutoffHeight("Cutoff Gradient", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+50" 
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "AuraMorph"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float4 screenPos : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _RimColor;
                float4 _NoiseTex_ST;
                float _OutlineWidth;
                float _DisplacementStrength;
                float _VerticalRise;
                float _ZOffset;
                float _NoiseScale;
                float _FlowSpeedX;
                float _FlowSpeedY;
                float _RimPower;
                float _RimSharpness;
                float _CutoffHeight;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 positionWS = TransformObjectToWorld(input.vertex.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normal);

                float2 noiseUV = (positionWS.xz * _NoiseScale) + (_Time.y * float2(_FlowSpeedX, _FlowSpeedY));
                float noiseVal = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, noiseUV, 0).r;

                float3 waveOffset = normalWS * (noiseVal * _DisplacementStrength);
                
                float3 verticalOffset = float3(0, _VerticalRise * noiseVal, 0);

                float3 finalPosWS = positionWS + (normalWS * _OutlineWidth) + waveOffset + verticalOffset;

                output.positionCS = TransformWorldToHClip(finalPosWS);

                #if UNITY_REVERSED_Z
                    output.positionCS.z -= _ZOffset * 0.02;
                #else
                    output.positionCS.z += _ZOffset * 0.02;
                #endif

                output.normalWS = normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(finalPosWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 noiseUV = (screenUV * _NoiseScale) - (_Time.y * float2(_FlowSpeedX, _FlowSpeedY));
                
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = 1.0 - NdotV;
                
                float rim = pow(fresnel, _RimPower);
                
                rim = saturate(rim - noise);
                
                float innerHardness = saturate(rim * _RimSharpness);
                float outerHardness = saturate((rim + 0.1) * _RimSharpness);
                
                float finalAlphaMask = outerHardness; 
                float borderMask = outerHardness - innerHardness;

                float4 finalColor = lerp(_CoreColor, _RimColor, borderMask);
                
                finalColor.a *= finalAlphaMask;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
