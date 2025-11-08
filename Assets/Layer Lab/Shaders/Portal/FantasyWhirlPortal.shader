Shader "Portal/FantasyWhirlURP"
{
    Properties
    {
        _NoiseTex("Noise Texture (RG)", 2D) = "white" {}
        _GradientMap("Gradient Map", 2D) = "white" {}
        _WhirlStrength("Whirl Strength", Range(0, 100)) = 1.0
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 2.0
        _NoiseSpeed("Noise Speed", Range(0, 5)) = 0.5
        _CenterColor("Center Color", Color) = (1, 1, 1, 1)
        _EdgeColor("Edge Color", Color) = (0, 0, 0, 1)
        _Falloff("Edge Falloff", Range(0, 5)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_GradientMap);
            SAMPLER(sampler_GradientMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _NoiseTex_ST;
            float _WhirlStrength;
            float _NoiseScale;
            float _NoiseSpeed;
            float4 _CenterColor;
            float4 _EdgeColor;
            float _Falloff;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float2 convertToPolar(float2 cartesian)
            {
                float radius = length(cartesian);
                float angle = atan2(cartesian.y, cartesian.x);
                return float2(radius, angle);
            }

            float readNoise(float2 uv, float timeOffset)
            {
                float2 scrolledUV = uv + float2(timeOffset, 0);
                return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, scrolledUV).r;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 centeredUV = (IN.uv - 0.5) * 2.0;
                float2 polarCoords = convertToPolar(centeredUV);

                float radius = polarCoords.x;
                float angle = polarCoords.y;

                float time = _Time.y * _NoiseSpeed;

                float whirlOffset = radius * _WhirlStrength;
                angle += whirlOffset - time;

                float2 noiseUV = float2(angle / (2.0 * PI), radius);
                noiseUV *= _NoiseScale;

                float noiseValue = readNoise(noiseUV, time * 0.2);

                float gradientSampleU = noiseValue;
                float4 portalColor = SAMPLE_TEXTURE2D(_GradientMap, sampler_GradientMap, float2(gradientSampleU, 0.5));

                float edgeFactor = saturate(radius * _Falloff);
                float4 finalColor = lerp(_CenterColor, _EdgeColor, edgeFactor);
                finalColor *= portalColor;

                float alpha = saturate(1.0 - pow(radius, 2.0));
                alpha *= portalColor.a;

                return float4(finalColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}
