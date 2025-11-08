Shader "CleanCode/TrueRasengan"
{
    Properties
    {
        [Header(Core and Flow)]
        _CoreColor("Core Color", Color) = (1, 1, 1, 1)
        _FlowColor("Flow Color", Color) = (0.5, 0.8, 1, 1)
        _CoreRadius("Core Radius", Range(0.01, 1.0)) = 0.25
        _CoreIntensity("Core Intensity", Range(1.0, 20.0)) = 10.0

        [Header(Surface Scribbles)]
        _ScribbleTex("Scribble Texture (Voronoi/Scratches)", 2D) = "white" {}
        _ScribbleColor("Scribble Color", Color) = (1, 1, 1, 1)
        _ScribbleSpeed("Scribble Speed", Float) = 2.5
        _ScribbleDensity("Scribble Density", Range(0, 10)) = 4.0
        _ScribbleSharpness("Scribble Sharpness", Range(1.0, 100.0)) = 10.0

        [Header(Outer Shell)]
        _ShellColor("Shell Color (Rim)", Color) = (0.2, 0.5, 1, 1)
        _ShellFresnelPower("Shell Fresnel Power", Range(1.0, 50.0)) = 15.0
        _ShellVisibility("Shell Visibility", Range(0.0, 1.0)) = 0.8

        [Header(Internal Flow Noise)]
        _FlowNoiseTex("Flow Noise Texture (Perlin)", 2D) = "white" {}
        _FlowNoiseSpeed("Flow Noise Speed", Vector) = (0.1, 0.05, -0.08, 0.12)
        _FlowNoiseScale("Flow Noise Scale", Float) = 1.5
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "DisableBatching" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            sampler2D _ScribbleTex;
            float4 _ScribbleTex_ST;
            sampler2D _FlowNoiseTex;
            float4 _FlowNoiseTex_ST;

            float4 _CoreColor, _FlowColor, _ScribbleColor, _ShellColor;
            float _CoreRadius, _CoreIntensity, _ScribbleSpeed, _ScribbleDensity, _ScribbleSharpness;
            float _ShellFresnelPower, _ShellVisibility;
            float4 _FlowNoiseSpeed;
            float _FlowNoiseScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                // Use raw UVs for calculations
                return o;
            }

            float calculateFresnel(float3 worldPos, float3 worldNormal)
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                float dotProduct = 1.0 - saturate(dot(viewDir, worldNormal));
                return pow(dotProduct, _ShellFresnelPower);
            }

            float3 createChaoticFlow(float2 uv, float time)
            {
                float2 uv1 = uv * _FlowNoiseScale + time * _FlowNoiseSpeed.xy;
                float2 uv2 = uv * _FlowNoiseScale * 1.5 + time * _FlowNoiseSpeed.zw;

                float noise1 = tex2D(_FlowNoiseTex, uv1).r;
                float noise2 = tex2D(_FlowNoiseTex, uv2).r;

                return _FlowColor.rgb * (noise1 + noise2) * 0.5;
            }

            float createScribbles(float2 uv, float time)
            {
                float2 scribbleUV1 = uv * _ScribbleDensity + float2(time * _ScribbleSpeed, 0);
                float2 scribbleUV2 = uv * _ScribbleDensity + float2(0, time * _ScribbleSpeed * -1.2);
                float2 scribbleUV3 = (uv.yx * _ScribbleDensity * 0.8) + float2(time * _ScribbleSpeed * 0.8, 0);

                float scribble1 = tex2D(_ScribbleTex, scribbleUV1).r;
                float scribble2 = tex2D(_ScribbleTex, scribbleUV2).r;
                float scribble3 = tex2D(_ScribbleTex, scribbleUV3).r;

                // Combine and sharpen the lines
                float combined = scribble1 * scribble2 * scribble3;
                return pow(1.0 - combined, _ScribbleSharpness);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float fresnel = calculateFresnel(i.worldPos, i.worldNormal);

                // 1. Calculate the bright core
                float2 centeredUV = i.uv - 0.5;
                float coreMask = 1.0 - saturate(length(centeredUV) / _CoreRadius);
                coreMask = pow(coreMask, 2.0);
                // Sharpen the core falloff
                float3 core = _CoreColor.rgb * coreMask * _CoreIntensity;

                // 2. Calculate the chaotic internal flow
                float3 flow = createChaoticFlow(i.uv, _Time.y);

                // 3. Calculate the surface scribbles
                float scribbleMask = createScribbles(i.uv, _Time.y);
                float3 scribbles = _ScribbleColor.rgb * scribbleMask;

                // 4. Combine inner parts, masked by view angle
                float viewFade = 1.0 - pow(fresnel, 0.25);
                // Core and flow fade at the edges
                float3 internalEnergy = (flow + core) * viewFade;

                // 5. Define the outer shell
                float3 shell = _ShellColor.rgb * fresnel * _ShellVisibility;

                // 6. Layer everything together
                float3 finalColor = internalEnergy + shell + (scribbles * viewFade);

                // 7. Define final alpha
                float alpha = saturate(fresnel * _ShellVisibility + scribbleMask * viewFade + coreMask * viewFade);

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
