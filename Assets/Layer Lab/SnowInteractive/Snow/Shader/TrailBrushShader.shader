Shader "Hidden/TrailBlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrushCenterUV ("Brush Center UV", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius", Float) = 0.1
        _BrushStrength ("Brush Strength", Range(0, 1)) = 1.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _BrushCenterUV;
            float _BrushRadius;
            float _BrushStrength;

            fixed4 frag (v2f i) : SV_Target
            {
                // Lấy màu gốc từ trail texture
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // Tính toán giá trị của brush tại pixel hiện tại
                float dist = distance(i.uv, _BrushCenterUV.xy);
                float brushValue = 1.0 - smoothstep(_BrushRadius - 0.01, _BrushRadius, dist);
                brushValue *= _BrushStrength;

                // Lấy giá trị lớn hơn giữa màu gốc và brush
                // Điều này đảm bảo tuyết chỉ lún xuống, không bao giờ __string__0__endstring__ lên
                // và các vết lún sâu hơn sẽ đè lên vết nông hơn
                float finalValue = max(originalColor.g, brushValue);

                // Chỉ sử dụng kênh Green (G) để lưu độ sâu, các kênh khác không cần thiết
                return fixed4(0, finalValue, 0, 1);
            }
            ENDHLSL
        }
    }
}
