Shader "Enhanced/CylinderGodRay"
{
    Properties
    {
        [Header(Main Properties)]
        [HDR] _TopColor("Top Color", Color) = (1, 0.9, 0.7, 0.5)
        [HDR] _BottomColor("Bottom Color", Color) = (1, 0.5, 0, 0)
        _Intensity("Intensity", Range(0, 50)) = 10.0

        [Header(Shape and Falloff)]
        _RimPower("Rim Power", Range(0.1, 20.0)) = 3.0
        _VerticalFalloffPower("Vertical Falloff Power", Range(-10.0, 10.0)) = 2.0
        _HorizontalFalloffPower("Horizontal Falloff Power", Range(0.1, 20.0)) = 1.0

        [Header(Noise and Animation)]
        _NoiseTex("Noise Texture (Grayscale)", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 1.0
        _NoiseSpeed("Noise Scroll Speed (X, Y)", Vector) = (0.1, 0.1, 0, 0)
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.5
        _PulseFrequency("Pulse Frequency", Range(0, 20)) = 5.0
        _PulseAmplitude("Pulse Amplitude", Range(0, 1)) = 0.2

        [Header(Performance and Fading)]
        _DepthFadeDistance("Depth Fade Distance", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True" }

        Pass
        {
            Blend One One
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Others/MathUtils.hlsl"
        

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS_HCS      : SV_POSITION;
                float3 normalWS_normalized : TEXCOORD0;
                float3 viewDir_normalized  : TEXCOORD1;
                float2 uv                  : TEXCOORD2;
                float4 screenPos           : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;
                half _Intensity;
                half _RimPower;
                half _VerticalFalloffPower;
                half _HorizontalFalloffPower;
                half _NoiseScale;
                half2 _NoiseSpeed;
                half _NoiseStrength;
                half _PulseFrequency;
                half _PulseAmplitude;
                half _DepthFadeDistance;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS_HCS = TransformWorldToHClip(positionWS);
                
                output.normalWS_normalized = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.viewDir_normalized = normalize(GetWorldSpaceViewDir(positionWS));
                
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS_HCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half dotNV = dot(input.normalWS_normalized, input.viewDir_normalized);
                
                half rim = 1.0h - saturate(dotNV);
                half rimFalloff = MU_FastPow(rim, _RimPower);
                
                half verticalFalloff = MU_FastPow(input.uv.y, _VerticalFalloffPower);
                half horizontalFalloff = MU_FastPow(saturate(dotNV), _HorizontalFalloffPower);

                float2 noiseUV = input.uv * _NoiseScale + _Time.y * _NoiseSpeed;
                half noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                half noiseEffect = lerp(1.0h, noiseValue, _NoiseStrength);
                
                half pulseEffect = 1.0h - ((sin(_Time.y * _PulseFrequency) * 0.5h + 0.5h) * _PulseAmplitude);
                
                float sceneDepth = SampleSceneDepth(input.screenPos.xy / input.screenPos.w);
                float effectDepth = input.screenPos.w;
                float depthDifference = sceneDepth - effectDepth;
                half depthFade = saturate(depthDifference / _DepthFadeDistance);

                half3 gradientColor = lerp(_BottomColor.rgb, _TopColor.rgb, input.uv.y);
                half masterAlpha = lerp(_BottomColor.a, _TopColor.a, input.uv.y);

                half finalAlpha = rimFalloff * verticalFalloff * horizontalFalloff * noiseEffect * pulseEffect * masterAlpha * depthFade;
                half3 finalColor = gradientColor * finalAlpha * _Intensity;
                
                return half4(finalColor, 0);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}