Shader "BillEnv/URP/Interactive Snow"
{
    Properties
    {
        [Header(Main)]
        [MainColor] _SnowColor("Snow Color", Color) = (0.8, 0.85, 0.9, 1.0)
        _MainTex("Snow Texture", 2D) = "white" {}
        _SnowTextureOpacity("Snow Texture Opacity", Range(0, 1)) = 0.3
        _SnowTextureScale("Snow Texture Scale", Range(0, 2)) = 0.3
        _Normal("Snow Normal Map", 2D) = "bump" {}
        _SnowNormalStrength("Snow Normal Strength", Range(0, 1)) = 0.5
        [HDR] _ShadowColor("Shadow Color", Color) = (0.5, 0.5, 0.6, 1)

        [Header(Snow Shape)]
        _NoiseTexture("Snow Noise", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Range(0, 2)) = 0.1
        _NoiseWeight("Noise Weight", Range(0, 2)) = 0.1
        _SnowHeight("Snow Height", Range(0, 2)) = 0.3

        [Header(Tessellation)]
        _TessellationFactor("Tessellation Factor", Range(1, 64)) = 15
        _MaxTessellationDistance("Max Tessellation Distance", Range(10, 200)) = 50

        [Header(Interactive Path)]
        [HDR] _PathColorIn("Path Inner Color", Color) = (0.7, 0.7, 1.0, 1)
        [HDR] _PathColorOut("Path Outer Color", Color) = (0.4, 0.4, 0.8, 1)
        _PathBlending("Path Blending", Range(0, 3)) = 2
        _SnowPathStrength("Path Strength", Range(0, 4)) = 2

        [Header(Effects)]
        _SparkleNoise("Sparkle Noise", 2D) = "gray" {}
        _SparkleScale("Sparkle Scale", Range(0, 10)) = 10
        _SparkCutoff("Sparkle Cutoff", Range(0.0, 1.0)) = 0.95
        [HDR] _RimColor("Rim Color", Color) = (0.7, 0.75, 0.8, 1)
        _RimPower("Rim Power", Range(0, 20)) = 10
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma hull Hull
            #pragma domain Domain
            #pragma target 4.6

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Includes/SnowFunctions.hlsl"

            [domain("tri")]
            Varyings Domain(
            TessellationFactors tessFactors,
            const OutputPatch < Varyings, 3> patch,
            float3 barycentricCoords : SV_DomainLocation)
            {
                Varyings OUT;
                InterpolateVaryings(OUT, patch, barycentricCoords);
                ApplyVertexDisplacement(OUT);
                return OUT;
            }

            float4 Fragment(Varyings IN) : SV_Target
            {
                return CalculateFragmentColor(IN);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma hull Hull
            #pragma domain Domain
            #pragma target 4.6
            #include "Includes/SnowFunctions.hlsl"

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            [domain("tri")]
            ShadowVaryings Domain(
            TessellationFactors tessFactors,
            const OutputPatch < Varyings, 3> patch,
            float3 barycentricCoords : SV_DomainLocation)
            {
                Varyings interpolatedData;
                InterpolateVaryings(interpolatedData, patch, barycentricCoords);
                ApplyVertexDisplacement(interpolatedData);

                ShadowVaryings OUT;
                OUT.positionCS = interpolatedData.positionCS;
                return OUT;
            }

            float4 Fragment(ShadowVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }
        LOD 100

        Pass
        {
            Name "ForwardLitSimple"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _SnowColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(IN.normalWS, mainLight.direction));
                float3 finalColor = _SnowColor.rgb * mainLight.color * NdotL;
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
    CustomEditor "InteractiveSnowShaderGUI"
}
