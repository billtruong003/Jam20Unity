Shader "MinionsArt/URPWaterBlingSparkle"
{
    Properties
    {
        [Header(Colors and Depth)]
        _ShallowColor("Shallow Color", Color) = (0.3, 0.8, 0.9, 0.7)
        [HDR] _DeepColor("Deep Color", Color) = (0.0, 0.2, 0.4, 0.8)
        _DepthMaxDistance("Depth Max Distance", Range(0, 10)) = 3.0

        [Header(Surface Normals and Refraction)]
        _NormalMapA("Normal A", 2D) = "bump" {}
        _NormalTilingA("Normal Tiling A", Float) = 0.8
        _NormalScrollA("Normal Scroll A", Vector) = (0.01, 0.01, 0, 0)
        _NormalMapB("Normal B", 2D) = "bump" {}
        _NormalTilingB("Normal Tiling B", Float) = 1.2
        _NormalScrollB("Normal Scroll B", Vector) = (-0.012, 0.008, 0, 0)
        _RefractionStrength("Refraction Strength", Range(0.0, 0.1)) = 0.025

        [Header(Surface Foam)]
        _SurfaceFoamTexture("Surface Foam Texture", 2D) = "white" {}
        _SurfaceFoamTiling("Surface Foam Tiling", Float) = 1.0
        _SurfaceFoamScroll("Surface Foam Scroll", Vector) = (0.02, 0.025, 0, 0)
        _SurfaceFoamCutoff("Surface Foam Cutoff", Range(0, 1)) = 0.5
        _SurfaceFoamDistortionMap("Foam Distortion Map", 2D) = "gray" {}
        _SurfaceFoamDistortionStrength("Foam Distortion Strength", Range(0, 0.2)) = 0.05

        [Header(Bling Sparkle)]
        _BlingNoiseMap("Bling Noise Map (Spatial)", 2D) = "white" {}
        [HDR] _BlingColor("Bling Color", Color) = (1.5, 1.5, 1.5, 1.0)
        _BlingIntensity("Bling Intensity", Range(0, 10)) = 2.0
        _BlingScale("Bling Scale (Screen Space)", Float) = 2.0
        _BlingSpeed("Bling Speed (Temporal)", Range(0, 20)) = 10.0
        _BlingFresnelPower("Bling Fresnel Power", Range(1, 20)) = 8.0
        _BlingThreshold("Bling Threshold", Range(0.5, 1.0)) = 0.98

        [Header(Intersection Foam)]
        _FoamColor("Intersection Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FoamIntersectionDepth("Intersection Depth", Range(0.01, 5.0)) = 0.5
        _FoamIntersectionSoftness("Intersection Softness", Range(0.01, 5.0)) = 1.0

        [Header(Vertex Waves)]
        _WaveAmplitude("Wave Amplitude", Range(0.0, 1.0)) = 0.1
        _WaveFrequency("Wave Frequency", Float) = 1.0
        _WaveSpeed("Wave Speed", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor, _DeepColor, _FoamColor, _BlingColor;
            float4 _NormalScrollA, _NormalScrollB, _SurfaceFoamScroll;
            float _DepthMaxDistance, _NormalTilingA, _NormalTilingB, _RefractionStrength;
            float _SurfaceFoamTiling, _SurfaceFoamCutoff, _SurfaceFoamDistortionStrength;
            float _BlingScale, _BlingSpeed, _BlingFresnelPower, _BlingThreshold, _BlingIntensity;
            float _FoamIntersectionDepth, _FoamIntersectionSoftness;
            float _WaveAmplitude, _WaveFrequency, _WaveSpeed;
            CBUFFER_END

            TEXTURE2D(_NormalMapA);
            SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB);
            SAMPLER(sampler_NormalMapB);
            TEXTURE2D(_SurfaceFoamTexture);
            SAMPLER(sampler_SurfaceFoamTexture);
            TEXTURE2D(_SurfaceFoamDistortionMap);
            SAMPLER(sampler_SurfaceFoamDistortionMap);
            TEXTURE2D(_BlingNoiseMap);
            SAMPLER(sampler_BlingNoiseMap);

            half hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float time = _Time.y * _WaveSpeed;
                float waveValue = sin(time + (input.positionOS.x + input.positionOS.z) * _WaveFrequency) * _WaveAmplitude;
                float4 modifiedPositionOS = input.positionOS;
                modifiedPositionOS.y += waveValue;
                output.positionWS = TransformObjectToWorld(modifiedPositionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float rawSceneDepth = SampleSceneDepth(input.screenPos.xy / input.screenPos.w);
                float sceneLinearEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float surfaceLinearEyeDepth = input.screenPos.w;
                float depthDifference = sceneLinearEyeDepth - surfaceLinearEyeDepth;
                float depthBlendFactor = saturate(depthDifference / _DepthMaxDistance);
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthBlendFactor);

                float2 normalUVA = input.positionWS.xz * _NormalTilingA + _Time.y * _NormalScrollA.xy;
                float2 normalUVB = input.positionWS.xz * _NormalTilingB + _Time.y * _NormalScrollB.xy;
                half3 normalSampleA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, normalUVA));
                half3 normalSampleB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, normalUVB));
                half3 combinedNormal = normalize(half3(normalSampleA.xy + normalSampleB.xy, 2.0));

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 refractionUVOffset = combinedNormal.xy * _RefractionStrength;
                half3 refractionColor = SampleSceneColor(screenUV + refractionUVOffset);
                half3 colorWithRefraction = lerp(waterColor, refractionColor, 1 - _ShallowColor.a);

                float2 distortionUV = input.positionWS.xz * 0.25 + _Time.y * 0.01;
                float2 distortionOffset = (SAMPLE_TEXTURE2D(_SurfaceFoamDistortionMap, sampler_SurfaceFoamDistortionMap, distortionUV).xy * 2.0 - 1.0) * _SurfaceFoamDistortionStrength;
                float2 foamSurfaceUV = input.positionWS.xz * _SurfaceFoamTiling + _Time.y * _SurfaceFoamScroll.xy + distortionOffset;
                float foamSurfaceNoise = SAMPLE_TEXTURE2D(_SurfaceFoamTexture, sampler_SurfaceFoamTexture, foamSurfaceUV).r;
                float surfaceFoamAmount = step(_SurfaceFoamCutoff, foamSurfaceNoise);

                float foamIntersectionFactor = 1.0 - saturate(depthDifference / _FoamIntersectionDepth);
                foamIntersectionFactor = smoothstep(0.0, _FoamIntersectionSoftness, foamIntersectionFactor);
                float combinedFoamAmount = saturate(foamIntersectionFactor + surfaceFoamAmount);
                half3 colorWithFoam = lerp(colorWithRefraction, _FoamColor.rgb, combinedFoamAmount);

                float2 blingScreenUV = (input.positionCS.xy / input.positionCS.w) * _BlingScale;
                blingScreenUV.x *= _ScreenParams.x / _ScreenParams.y;
                half spatialNoise = SAMPLE_TEXTURE2D(_BlingNoiseMap, sampler_BlingNoiseMap, blingScreenUV).r;
                float timeStep = floor(_Time.y * _BlingSpeed);
                half temporalNoise = hash(float3(input.positionWS.xy * 0.1, timeStep));
                half combinedNoise = spatialNoise * temporalNoise;

                half3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                half NdotV = 1.0h - saturate(dot(combinedNormal, viewDirWS));
                half fresnel = pow(NdotV, _BlingFresnelPower);

                half sparkle = smoothstep(_BlingThreshold, _BlingThreshold + 0.01h, combinedNoise);
                half3 blingContribution = sparkle * fresnel * _BlingColor.rgb * _BlingIntensity;

                half3 finalColor = colorWithFoam + blingContribution;

                return half4(finalColor, _ShallowColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}
