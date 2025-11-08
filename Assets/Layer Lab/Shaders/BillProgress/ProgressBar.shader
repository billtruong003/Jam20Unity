Shader "Unlit/ProductionReadyHealthBar"
{
    Properties
    {
        [Header(Fill and Shape)]
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 0.5
        _BorderThickness ("Border Thickness (Pixels)", Range(0.0, 50.0)) = 3.0
        _CornerRadius ("Corner Radius (Pixels)", Range(0.0, 100.0)) = 15.0

        [Header(Colors)]
        _BackgroundColor ("Background Color", Color) = (0.1, 0.1, 0.1, 1)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 1)
        [HDR] _FullColor ("Full Health Color", Color) = (0.1, 0.8, 0.2, 1)
        [HDR] _LowColor ("Low Health Color", Color) = (0.8, 0.1, 0.1, 1)
        _DepthGradient("Liquid Depth Gradient", Range(0.0, 1.0)) = 0.3

        [Header(Wave Settings)]
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _WaveSpeed1("Wave 1 Speed", Float) = 1.0
        _WaveFrequency1("Wave 1 Frequency", Float) = 10.0
        _WaveAmplitude1("Wave 1 Amplitude", Range(0.0, 0.1)) = 0.05

        _WaveSpeed2("Wave 2 Speed", Float) = 0.6
        _WaveFrequency2("Wave 2 Frequency", Float) = 5.0
        _WaveAmplitude2("Wave 2 Amplitude", Range(0.0, 0.1)) = 0.03
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite On
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _NoiseTex;

            half _FillAmount;
            float _BorderThickness;
            float _CornerRadius;

            fixed4 _BackgroundColor;
            fixed4 _BorderColor;
            fixed4 _FullColor;
            fixed4 _LowColor;
            half _DepthGradient;

            float _WaveSpeed1, _WaveFrequency1, _WaveAmplitude1;
            float _WaveSpeed2, _WaveFrequency2, _WaveAmplitude2;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float get_rounded_rect_sdf(float2 positionFromCenter, float2 halfSize, float radius)
            {
                float2 distance = abs(positionFromCenter) - halfSize + radius;
                return length(max(distance, 0.0)) - radius;
            }

            float get_wave_offset(float2 uv, float speed, float frequency, float amplitude)
            {
                uv.x *= frequency;
                uv.x += _Time.y * speed;
                return (tex2D(_NoiseTex, uv).r - 0.5) * amplitude;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 rectSizeInPixels = 1.0 / fwidth(i.uv);
                float2 pixelCoord = i.uv * rectSizeInPixels;
                float2 halfSize = 0.5 * rectSizeInPixels;
                float2 positionFromCenter = pixelCoord - halfSize;

                float antialias = 1.0;

                float outerSDF = get_rounded_rect_sdf(positionFromCenter, halfSize, _CornerRadius);
                float shapeMask = 1.0 - smoothstep(-antialias, antialias, outerSDF);
                clip(shapeMask - 0.01);

                float innerRadius = max(0.0, _CornerRadius - _BorderThickness);
                float2 innerHalfSize = max(float2(0, 0), halfSize - _BorderThickness);
                float innerSDF = get_rounded_rect_sdf(positionFromCenter, innerHalfSize, innerRadius);
                float contentMask = 1.0 - smoothstep(-antialias, antialias, innerSDF);

                float waveOffset1 = get_wave_offset(i.uv, _WaveSpeed1, _WaveFrequency1, _WaveAmplitude1);
                float waveOffset2 = get_wave_offset(i.uv, _WaveSpeed2, _WaveFrequency2, _WaveAmplitude2);
                float totalWaveOffset = waveOffset1 + waveOffset2;

                // --- THE FIX IS HERE ---
                // This line acts as an on/off switch. If _FillAmount is near zero, this evaluates
                // to 0, completely disabling the waves. Otherwise, it's 1.
                totalWaveOffset *= step(0.0001, _FillAmount);
                // --- END OF FIX ---

                float surfaceY = _FillAmount + totalWaveOffset;

                fixed4 contentColor = _BackgroundColor;
                if (i.uv.y < surfaceY)
                {
                    fixed4 liquidColor = lerp(_LowColor, _FullColor, saturate(_FillAmount * 1.2));
                    float depthFactor = 1.0 - saturate(surfaceY - i.uv.y) / _FillAmount * _DepthGradient;
                    liquidColor.rgb *= depthFactor;
                    contentColor = liquidColor;
                }

                fixed4 finalColor = contentColor * contentMask;
                finalColor = lerp(finalColor, _BorderColor, shapeMask - contentMask);
                finalColor.a = shapeMask;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
