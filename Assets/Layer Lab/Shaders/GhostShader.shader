Shader "Custom/UnlitToonGhost"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _FresnelColor ("Fresnel Color", Color) = (0, 0.8, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 2.5
        _Transparency ("Transparency", Range(0.0, 1.0)) = 0.5
        _ToonThreshold ("Toon Threshold", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Blend One One
            ZWrite On
            Cull Back
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 normalWS     : NORMAL;
                float3 viewDirWS    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _FresnelColor;
                float _FresnelPower;
                float _Transparency;
                float _ToonThreshold;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - TransformObjectToWorld(input.positionOS.xyz));
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Fresnel Effect
                float fresnelDot = dot(input.normalWS, input.viewDirWS);
                float fresnel = pow(1.0 - saturate(fresnelDot), _FresnelPower);
                fresnel = saturate(fresnel);

                // Toon Shading Logic
                Light mainLight = GetMainLight();
                float dotProduct = dot(input.normalWS, mainLight.direction);
                float toonStep = step(_ToonThreshold, dotProduct);
                
                // Combine Colors
                half4 baseColor = lerp(_Color, _FresnelColor, fresnel);
                half4 toonColor = lerp(baseColor * 0.5, baseColor, toonStep);

                // Final Output
                half4 finalColor = half4(toonColor.rgb, _Transparency);
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}