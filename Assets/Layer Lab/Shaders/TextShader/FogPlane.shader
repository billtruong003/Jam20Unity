Shader "Bill/Unlit/DepthFogURP"
{
    Properties
    {
        [HDR] _TintColor ("Fog Tint (RGBA)", Color) = (1, 1, 1, 0.5)
        _Strength ("Fog Strength", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            { float4 positionOS : POSITION;

            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float viewDepth : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _TintColor;
            half _Strength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.viewDepth = abs(positionVS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float planeDepth = input.viewDepth;
                half depthDifference = sceneDepth - planeDepth;
                half fogFactor = saturate(depthDifference * _Strength);
                half4 finalColor = _TintColor;
                finalColor.a *= fogFactor;
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
