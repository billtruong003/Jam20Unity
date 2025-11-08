Shader "Custom/PlacementPreviewURP"
{
    Properties
    {
        [Header(Main Settings)]
        _BaseColor("Base Color", Color) = (0, 0.5, 1, 0.5)

        [Header(Fresnel Effect)]
        _FresnelColor("Fresnel Color", Color) = (0.5, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.5
        _FresnelIntensity("Fresnel Intensity", Range(0.0, 5.0)) = 1.0

        [Header(Intersection Effect)]
        _IntersectionColor("Intersection Color", Color) = (1, 0, 0, 1)
        _IntersectionThreshold("Intersection Threshold", Range(0.0, 1.0)) = 0.05
        _IntersectionSmoothness("Intersection Smoothness", Range(0.0, 1.0)) = 0.02
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FresnelColor;
                half4 _IntersectionColor;
                float _FresnelPower;
                float _FresnelIntensity;
                float _IntersectionThreshold;
                float _IntersectionSmoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionCS.xy / IN.positionCS.w;
                float sceneRawDepth = SampleSceneDepth(screenUV);
                float sceneLinearDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float objectLinearDepth = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);

                float depthDifference = sceneLinearDepth - objectLinearDepth;

                float intersectionMask = saturate(depthDifference / _IntersectionThreshold);
                intersectionMask = 1.0 - smoothstep(0.0, _IntersectionSmoothness, intersectionMask);

                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float dotProduct = dot(viewDirectionWS, normalize(IN.normalWS));
                float fresnelTerm = pow(1.0 - saturate(dotProduct), _FresnelPower);
                fresnelTerm *= _FresnelIntensity;
                half3 fresnelColor = _FresnelColor.rgb * fresnelTerm;

                half3 finalColor = lerp(_BaseColor.rgb, _IntersectionColor.rgb, intersectionMask);
                finalColor += fresnelColor;

                half finalAlpha = _BaseColor.a * (1.0 - intersectionMask);

                if (depthDifference > 0 && depthDifference < _IntersectionThreshold)
                {
                    finalAlpha = 1.0;
                }

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}