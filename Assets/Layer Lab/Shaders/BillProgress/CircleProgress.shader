Shader "Unlit/ProductionReadyCircularHealthBar_V2"
{
    Properties
    {
        [Header(Fill and Shape)]
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 0.5
        _BorderThickness ("Border Thickness", Range(0.0, 0.5)) = 0.05
        _RotationStart("Rotation Start (Degrees)", Range(0, 360)) = 90

        [Header(Colors)]
        _BackgroundColor ("Background Color", Color) = (0.1, 0.1, 0.1, 1)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 1)
        [HDR] _FullColor ("Full Progress Color", Color) = (0.1, 0.8, 0.2, 1)
        [HDR] _LowColor ("Low Progress Color", Color) = (0.8, 0.1, 0.1, 1)

        [Header(Rendering Options)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4// LessEqual
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0// Off
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
            ZWrite [_ZWrite]
            ZTest [_ZTest]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define PI 3.14159265359

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

            half _FillAmount;
            half _BorderThickness;
            float _RotationStart;

            fixed4 _BackgroundColor;
            fixed4 _BorderColor;
            fixed4 _FullColor;
            fixed4 _LowColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float distance = length(centeredUV);
                float antialiasWidth = fwidth(distance);

                float shapeMask = 1.0 - smoothstep(1.0 - antialiasWidth, 1.0, distance);
                float contentRadius = 1.0 - _BorderThickness;
                float contentMask = 1.0 - smoothstep(contentRadius - antialiasWidth, contentRadius, distance);
                float borderMask = shapeMask - contentMask;

                float angle = atan2(centeredUV.y, centeredUV.x);
                float rotationInRadians = _RotationStart * PI / 180.0;
                float angleZeroToOne = (angle + rotationInRadians) / (2.0 * PI);
                angleZeroToOne = frac(angleZeroToOne);

                float fillMask = smoothstep(0.0, antialiasWidth * 2.0, _FillAmount - angleZeroToOne);

                fixed4 progressColor = lerp(_LowColor, _FullColor, saturate(_FillAmount * 1.2));

                fixed4 contentColor = lerp(_BackgroundColor, progressColor, fillMask);

                fixed4 finalColor = contentColor * contentMask;
                finalColor = lerp(finalColor, _BorderColor, borderMask);
                finalColor.a = shapeMask;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
