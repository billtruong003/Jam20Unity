Shader "CleanCode/ToonTransparentBubble"
{
    Properties
    {
        [Header(Main Settings)]
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Range(0.0, 1.0)] _Alpha("Alpha", Float) = 0.5

        [Header(Toon Lighting)]
        _ToonThreshold("Toon Threshold", Range(0.0, 1.0)) = 0.5

        [Header(Specular)]
        [HDR] _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        [Header(Rim Lighting)]
        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite On
            Ztest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Alpha;
            half _ToonThreshold;
            half4 _SpecularColor;
            half _Smoothness;
            half4 _RimColor;
            half _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.clipPos = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalDir = normalize(input.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.worldPos.xyz);

                Light mainLight = GetMainLight();

                half NdotL = saturate(dot(normalDir, mainLight.direction));
                half toonLightFactor = step(_ToonThreshold, NdotL);
                half3 toonLighting = toonLightFactor * mainLight.color;

                float3 halfDir = normalize(mainLight.direction + viewDir);
                half NdotH = saturate(dot(normalDir, halfDir));
                half specularPower = exp2(_Smoothness * 10.0 + 1.0);
                half3 specularHighlight = pow(NdotH, specularPower) * _SpecularColor.rgb * mainLight.color;

                half rimFactor = pow(1.0 - saturate(dot(normalDir, viewDir)), _RimPower);
                half3 rimLighting = rimFactor * _RimColor.rgb;

                half3 finalColor = _BaseColor.rgb * toonLighting;
                finalColor += specularHighlight;
                finalColor += rimLighting;

                return half4(finalColor, _Alpha);
            }
            ENDHLSL
        }
    }
}
