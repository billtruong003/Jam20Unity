Shader "Bill's Toon/Optimized/Transparent"
{
    Properties
    {
        [HideInInspector] _SurfaceType("Surface Type", Float) = 1

        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        [Header(Lighting)]
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)

        [Header(Stylized Glass)]
        _GlassColor("Glass Color & Opacity", Color) = (0.8, 0.9, 1.0, 0.5)
        _FresnelColor("Fresnel (Edge) Color", Color) = (1, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(1, 10)) = 5.0
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.01
        _GlassSpecularPower("Specular Power", Range(1, 50)) = 20.0
        _GlassSpecularIntensity("Specular Intensity", Range(0, 5)) = 1.0

        [Header(Outline Properties (Fresnel))]
        [Toggle(_OUTLINEMODE_FRESNEL)] _FresnelOutlineToggle("Enable Fresnel Outline", Float) = 0
        _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 20.0)) = 5.0
        _FresnelOutlineSharpness("Sharpness", Range(0.1, 10.0)) = 2.0

        [Toggle(_OUTLINEGLINT_ON)] _GlintToggle("Enable Glint Effect", Float) = 0
        [HDR] _GlintColor("Glint Color", Color) = (1, 1, 0.5, 1)
        _GlintScale("Glint Scale", Float) = 20.0
        _GlintSpeed("Glint Speed", Range(0.1, 10.0)) = 2.0
        _GlintThreshold("Glint Threshold", Range(0.5, 0.99)) = 0.95
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _SURFACETYPE_GLASS
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _OUTLINEGLINT_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Includes/Toon/ToonUberTransparentCore.hlsl"

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                Light mainLight = GetMainLight();
                half3 ambient = SampleSH(i.normalWS);

                half3 surfaceColor = CalculateGlassLighting(i, mainLight, viewDir, ambient);

                surfaceColor = ApplyEmission(surfaceColor, i.uv);
                surfaceColor = ApplyGlintEffect(surfaceColor, i.normalWS, viewDir, mainLight, i.positionCS.xy);
                surfaceColor = ApplyFresnelOutline(surfaceColor, i.normalWS, viewDir, i.positionWS);

                return half4(surfaceColor, _GlassColor.a);
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonTransparentShaderGUI"
}
