Shader "Custom/FullScreenFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.8, 0.9, 1, 1) // Màu sương mặc định (xanh nhạt)
        _FogNear ("Fog Near", Float) = 0.0 // Khoảng cách bắt đầu sương
        _FogFar ("Fog Far", Float) = 100.0 // Khoảng cách sương tối đa
        _Density ("Fog Density", Float) = 0.1 // Mật độ sương cho các chế độ lũy thừa
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Định nghĩa các từ khóa cho các chế độ sương mù
            #pragma multi_compile FOG_LINEAR_DEPTH FOG_EXP_DEPTH FOG_EXP2_DEPTH FOG_LINEAR_DISTANCE FOG_EXP_DISTANCE FOG_EXP2_DISTANCE

            // Bao gồm các thư viện URP cần thiết
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Cấu trúc đầu vào và đầu ra cho vertex shader
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Khai báo các biến đồng nhất (uniforms)
            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogNear;
                float _FogFar;
                float _Density;
            CBUFFER_END

            // Texture và sampler cho màu nguồn
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Vertex shader
            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Fragment shader
            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Lấy độ sâu từ depth texture
                float depth = SampleSceneDepth(uv);
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams); // Chuyển đổi thành độ sâu tuyến tính

                // Tính khoảng cách từ camera đến fragment
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float distance = length(worldPos - _WorldSpaceCameraPos);

                float coordinate;
                float fogFactor;

                // Tính toán fog factor dựa trên chế độ được chọn
                #if defined(FOG_LINEAR_DEPTH)
                    coordinate = linearDepth;
                    fogFactor = saturate((_FogFar - coordinate) / (_FogFar - _FogNear)); // Sương tuyến tính dựa trên độ sâu
                #elif defined(FOG_EXP_DEPTH)
                    coordinate = linearDepth;
                    fogFactor = 1.0 - exp(-_Density * coordinate); // Sương lũy thừa dựa trên độ sâu
                #elif defined(FOG_EXP2_DEPTH)
                    coordinate = linearDepth;
                    fogFactor = 1.0 - exp(-_Density * coordinate * coordinate); // Sương lũy thừa bậc hai dựa trên độ sâu
                #elif defined(FOG_LINEAR_DISTANCE)
                    coordinate = distance;
                    fogFactor = saturate((_FogFar - coordinate) / (_FogFar - _FogNear)); // Sương tuyến tính dựa trên khoảng cách
                #elif defined(FOG_EXP_DISTANCE)
                    coordinate = distance;
                    fogFactor = 1.0 - exp(-_Density * coordinate); // Sương lũy thừa dựa trên khoảng cách
                #elif defined(FOG_EXP2_DISTANCE)
                    coordinate = distance;
                    fogFactor = 1.0 - exp(-_Density * coordinate * coordinate); // Sương lũy thừa bậc hai dựa trên khoảng cách
                #endif

                // Trộn màu gốc với màu sương dựa trên fog factor
                half4 finalColor = lerp(color, _FogColor, fogFactor);
                return finalColor;
            }
            ENDHLSL
        }
    }
}