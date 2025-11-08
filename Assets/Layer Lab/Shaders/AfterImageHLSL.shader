Shader "URP/After Image Effect Enhanced"
{
    Properties
    {
        [Header(Appearance)]
        _MainTex("Main Texture (Optional)", 2D) = "white" {}
        [HDR]_Color("Base Color", Color) = (1,1,1,1)

        [Header(Rim Effect)]
        [HDR]_RimColor("Rim Color", Color) = (0,1,1,1)
        _RimPower("Rim Power", Range(1, 100)) = 20
        _RimIntensity("Rim Intensity", Range(0, 10)) = 1.0
        _RimPulseSpeed("Rim Pulse Speed", Range(0, 20)) = 5.0
        
        [Header(Dissolve Effect)]
        _Fade("Fade Amount", Range(-1,2)) = 1
        _NoiseTex("Noise Texture (Seamless)", 2D) = "white" {}
        // --- THAY ĐỔI BẮT ĐẦU ---
        _NoiseTiling("Noise Tiling (X, Y)", Vector) = (1, 1, 0, 0) // Thêm thuộc tính scale (tiling)
        // --- THAY ĐỔI KẾT THÚC ---
        _NoiseScrollSpeed("Noise Scroll Speed (X, Y)", Vector) = (0.1, 0.1, 0, 0)
        _DissolveEdgeWidth("Dissolve Edge Width", Range(0.0, 0.5)) = 0.1
        [HDR]_DissolveEdgeColor("Dissolve Edge Color", Color) = (1, 0, 0, 1)

        [Header(Distortion)]
        [Toggle(_DISTORTION_ON)] _EnableDistortion("Enable Distortion", Float) = 0
        _DistortionStrength("Distortion Strength", Range(0, 0.1)) = 0.01

        [Header(Render Settings)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Float) = 2 
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            // --- Render State ---
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // --- Keywords for togglable features ---
            #pragma shader_feature_local _DISTORTION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            // --- Texture Samplers ---
            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex);

            // --- Material Properties ---
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _RimPulseSpeed;
                float _Fade;
                // --- THAY ĐỔI BẮT ĐẦU ---
                float4 _NoiseTiling; // Khai báo biến tiling
                // --- THAY ĐỔI KẾT THÚC ---
                float2 _NoiseScrollSpeed;
                float _DissolveEdgeWidth;
                float4 _DissolveEdgeColor;
                float _DistortionStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(worldPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. DISTORTION
                float2 distortedUV = input.uv;
                #if _DISTORTION_ON
                    // Use the noise texture (or a different one) to create distortion
                    float2 distortionOffset = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv - _Time.y * _NoiseScrollSpeed * 0.5).xy * 2 - 1) * _DistortionStrength;
                    distortedUV += distortionOffset;
                #endif

                // 2. BASE COLOR
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                half4 finalColor = texColor * _Color;

                // 3. RIM LIGHTING
                half3 viewDir = normalize(input.viewDirWS);
                half3 normal = normalize(input.normalWS);
                half rim = 1.0 - saturate(dot(viewDir, normal));
                half rimEffect = pow(rim, _RimPower);
                
                // Pulsating effect for the rim
                half pulse = (sin(_Time.y * _RimPulseSpeed) * 0.5 + 0.5); // Remaps sin from [-1,1] to [0,1]
                finalColor.rgb += _RimColor.rgb * rimEffect * pulse * _RimIntensity;

                // 4. DISSOLVE EFFECT
                // --- THAY ĐỔI BẮT ĐẦU ---
                // Áp dụng tiling (scale) cho UV của noise texture trước khi cuộn (scroll)
                float2 noiseUV = (input.uv * _NoiseTiling.xy) + (_Time.y * _NoiseScrollSpeed);
                // --- THAY ĐỔI KẾT THÚC ---
                half noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                
                // Calculate the soft transition using smoothstep
                half clip = smoothstep(_Fade - _DissolveEdgeWidth, _Fade, noiseValue);

                // Isolate the edge to apply color
                half edgeFactor = 1.0 - smoothstep(_Fade, _Fade + _DissolveEdgeWidth, noiseValue);
                half edge = clip * edgeFactor;
                
                // Add the glowing edge color
                finalColor.rgb += _DissolveEdgeColor.rgb * edge * _DissolveEdgeColor.a; // Use edge color's alpha as intensity

                // Apply the final dissolve alpha
                finalColor.a *= clip;
                
                // Ensure the base color doesn't have 0 alpha if _Fade is 1
                finalColor.a = saturate(finalColor.a);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent"
}