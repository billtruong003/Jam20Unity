Shader "Custom/EnergyBall_URP"
{
    Properties
    {
        _MainColor ("Energy Color", Color) = (0, 1, 1, 1)
        _RimColor ("Rim Color", Color) = (0, 0.5, 1, 1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 1.0
        _DistortSpeed ("Distort Speed", Range(0, 5)) = 1.5
        _DistortScale ("Distort Scale", Range(0.1, 10)) = 3.0
        _RimPower ("Rim Power", Range(0.1, 10)) = 3.0
        _EmissiveIntensity ("Emissive Intensity", Range(1, 20)) = 8.0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 2.0
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        LOD 100
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "EnergyBallPass"
            Tags {"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                // UNITY_FOG_COORDS(4)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _RimColor;
                float4 _NoiseTex_ST;
                float _NoiseStrength;
                float _DistortSpeed;
                float _DistortScale;
                float _RimPower;
                float _EmissiveIntensity;
                float _PulseSpeed;
                float _PulseStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = normalInput.normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _NoiseTex);
                OUT.viewDirWS = GetWorldSpaceViewDir(OUT.positionWS);

                // UNITY_TRANSFER_FOG(OUT, OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Time
                float time = _Time.y;

                // UV distortion using noise
                float2 distortUV = IN.uv * _DistortScale;
                distortUV.x += time * _DistortSpeed * 0.1;
                distortUV.y += time * _DistortSpeed * 0.15;

                float noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortUV).r;
                float noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortUV * 1.7 + float2(time * 0.2, 0)).g;

                float noise = (noise1 + noise2) * 0.5;
                float energyMask = pow(noise, 2) * _NoiseStrength;

                // Pulsing
                float pulse = sin(time * _PulseSpeed) * _PulseStrength + (1.0 - _PulseStrength);
                energyMask *= pulse;

                // Rim / Fresnel
                float3 viewDir = normalize(IN.viewDirWS);
                float3 normal = normalize(IN.normalWS);
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower);

                // Combine
                float3 energyColor = _MainColor.rgb * energyMask;
                float3 rimColor = _RimColor.rgb * rim * 2.0;
                float3 finalColor = energyColor + rimColor;

                // Emissive boost
                finalColor *= _EmissiveIntensity;

                // Alpha
                float alpha = saturate(energyMask + rim * 0.8);

                // Fog
                // UNITY_APPLY_FOG(IN.fogCoord, finalColor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}