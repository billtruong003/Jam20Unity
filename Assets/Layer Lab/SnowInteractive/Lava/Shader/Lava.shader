// Tên file: AdvancedLavaURP.shader
Shader "Shmackle/AdvancedLavaURP"
{
    Properties
    {
        [Header(Main Flow)]
        [HDR] _Color("Dark Color", Color) = (0.5, 0.1, 0.1, 1)
        [HDR] _Color2("Bright Color", Color) = (1.0, 0.6, 0.1, 1)
        _MainTex("Flow Texture", 2D) = "white" {}
        _Scale("Flow Scale", Range(0, 1)) = 0.3
        _SpeedMainX("Flow Speed X", Range(-5, 5)) = 0.4
        _SpeedMainY("Flow Speed Y", Range(-5, 5)) = 0.4
        _Strength("Brightness", Range(0, 20)) = 3

        [Space(10)]
        [Header(Distortion)]
        _DistortTex("Distortion Texture", 2D) = "white" {}
        _ScaleDist("Distortion Scale", Range(0, 1)) = 0.5
        _SpeedDistortX("Distortion Speed X", Range(-5, 5)) = 0.2
        _SpeedDistortY("Distortion Speed Y", Range(-5, 5)) = 0.2
        _Distortion("Distortion Strength", Range(0, 1)) = 0.2
        _VertexDistortion("Vertex Color Distortion", Range(0, 1)) = 0.3

        [Space(10)]
        [Header(Vertex Movement)]
        _Speed("Wave Speed", Range(0, 1)) = 0.5
        _Amount("Wave Amount", Range(0, 1)) = 0.6
        _Height("Wave Height", Range(0, 1)) = 0.1

        [Space(10)]
        [Header(Intersection)]
        [HDR] _IntersectionColor("Intersection Emissive Color", Color) = (2.0, 1.5, 0.5, 1.0)
        _IntersectionDepth("Intersection Depth", Range(0.01, 5.0)) = 0.7
        _IntersectionSoftness("Intersection Softness", Range(0.01, 5.0)) = 1.5
        _IntersectionStrength("Intersection Strength", Range(1, 50)) = 15.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex LavaVertex
            #pragma fragment LavaFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 color : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _Color2;
            float _Scale;
            float _SpeedMainX;
            float _SpeedMainY;
            float _Strength;
            float _ScaleDist;
            float _SpeedDistortX;
            float _SpeedDistortY;
            float _Distortion;
            float _VertexDistortion;
            float _Speed;
            float _Amount;
            float _Height;
            half4 _IntersectionColor;
            float _IntersectionDepth;
            float _IntersectionSoftness;
            float _IntersectionStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DistortTex);
            SAMPLER(sampler_DistortTex);

            Varyings LavaVertex(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                float wave = sin(_Time.y * _Speed + (IN.positionOS.x * IN.positionOS.z * _Amount)) * _Height;
                IN.positionOS.y += wave * IN.color.r;

                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.color = IN.color;

                return OUT;
            }

            half4 LavaFragment(Varyings IN) : SV_Target
            {
                float rawSceneDepth = SampleSceneDepth(IN.screenPos.xy / IN.screenPos.w);
                float sceneLinearEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float surfaceLinearEyeDepth = IN.screenPos.w;
                float depthDifference = sceneLinearEyeDepth - surfaceLinearEyeDepth;

                float intersectionFactor = 1.0 - saturate(depthDifference / _IntersectionDepth);
                intersectionFactor = smoothstep(0.0, _IntersectionSoftness, intersectionFactor);

                float2 worldXZ = IN.positionWS.xz;

                float2 distortionScroll = _Time.x * float2(_SpeedDistortX, _SpeedDistortY);
                float2 distortionUV = worldXZ * _ScaleDist + distortionScroll;
                half distortionSample = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortionUV).r;

                float2 mainScroll = _Time.x * float2(_SpeedMainX, _SpeedMainY);
                float2 vertexDistortion = IN.color.r * _VertexDistortion;
                float2 mainUV = worldXZ * _Scale + mainScroll + vertexDistortion;
                mainUV += distortionSample * _Distortion;

                half mainTextureSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV).r;

                half3 baseColor = lerp(_Color.rgb, _Color2.rgb, mainTextureSample);
                half3 emissiveLava = baseColor * _Strength * IN.color.r;

                half3 intersectionEffect = _IntersectionColor.rgb * intersectionFactor * _IntersectionStrength;

                half3 finalEmissiveColor = emissiveLava + intersectionEffect;

                return half4(finalEmissiveColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/Unlit"
}
