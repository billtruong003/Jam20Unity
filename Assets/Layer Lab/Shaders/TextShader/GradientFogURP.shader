Shader "Bill/Unlit/GradientFogURP"
{
    Properties
    {
        [HDR] _TintColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _GradientStart ("Gradient Start", Range(0, 1)) = 0.0
        _GradientEnd ("Gradient End", Range(0, 1)) = 1.0
        _Intensity ("Intensity", Range(0, 5)) = 1.0
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                // Cần UV để xác định vị trí trên mặt phẳng
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _TintColor;
            half _GradientStart;
            half _GradientEnd;
            half _Intensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sử dụng tọa độ UV theo chiều dọc (uv.y) để tạo gradient
                // smoothstep tạo ra một sự chuyển đổi mượt mà
                half gradientFactor = smoothstep(_GradientStart, _GradientEnd, input.uv.y);

                half4 finalColor = _TintColor;
                // Độ alpha của màu cuối cùng được quyết định bởi gradient và intensity
                finalColor.a = saturate(gradientFactor * _TintColor.a * _Intensity);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
