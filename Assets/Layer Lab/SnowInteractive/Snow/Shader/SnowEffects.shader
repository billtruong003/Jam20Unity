// Assets/Scenes/SnowInteractive/Snow/Shader/SnowEffects.shader

Shader "Hidden/SnowEffects"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Healing
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_heal
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
            float _HealingRate;
            float _DeltaTime;

            fixed4 frag_heal (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // Reduce the green channel value over time, but not below zero.
                col.g = max(0, col.g - _HealingRate * _DeltaTime);
                return col;
            }
            ENDHLSL
        }

        // Pass 1: Smoothing
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_smooth
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
            // Represents the target state for the current frame
            sampler2D _PreviousFrameRT;
            // The final state from the last frame
            float _SmoothingLerpFactor;

            fixed4 frag_smooth (v2f i) : SV_Target
            {
                fixed4 currentState = tex2D(_MainTex, i.uv);
                fixed4 previousState = tex2D(_PreviousFrameRT, i.uv);

                // Linearly interpolate from the previous state to the current state
                return lerp(previousState, currentState, _SmoothingLerpFactor);
            }
            ENDHLSL
        }
    }
}
