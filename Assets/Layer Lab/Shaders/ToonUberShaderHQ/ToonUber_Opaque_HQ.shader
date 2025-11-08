Shader "Bill's Toon/Opaque HQ"
{
    Properties
    {
        [Header(Render States)]
        [Enum(Opaque, 0, Cutout, 1, Transparent, 2)] _RenderMode ("Render Mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10
        [Toggle] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Culling Mode", Float) = 2

        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Intensity", Range(0.0, 2.0)) = 1.0

        [Header(Alpha Clipping)]
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        [Header(Lighting)]
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)

        [Header(Toon Shading HQ)]
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        _MidtoneColor("Mid-tone Color", Color) = (0.6, 0.6, 0.6, 1.0)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.4
        _MidtoneThreshold("Mid-tone Threshold", Range(0, 1)) = 0.8
        _RampSmoothness("Ramp Smoothness", Range(0.001, 0.5)) = 0.05
        _AmbientColor("Ambient Color", Color) = (0.5, 0.5, 0.5, 0)

        [Header(Additional Lights Toon Shading)]
        _AddLightShadowTint("Shadow Tint (Additional)", Color) = (0.2, 0.2, 0.3, 1.0)
        _AddLightMidtoneColor("Mid-tone Color (Additional)", Color) = (0.7, 0.7, 0.7, 1.0)
        _AddLightShadowThreshold("Shadow Threshold (Additional)", Range(0, 1)) = 0.1
        _AddLightMidtoneThreshold("Mid-tone Threshold (Additional)", Range(0, 1)) = 0.6
        _AddLightRampSmoothness("Ramp Smoothness (Additional)", Range(0.001, 0.5)) = 0.1

        [Header(Stylized Metal)]
        _Ramp("Toon Ramp (RGB)", 2D) = "white" {}
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3
        _Offset("Specular Size", Range(0, 1)) = 0.8
        [HDR] _SpecuColor("Specular Color", Color) = (0.8, 0.45, 0.2, 1)
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9
        [HDR] _HiColor("Highlight Color", Color) = (1, 1, 1, 1)
        [HDR] _RimColor("Rim Color", Color) = (1, 0.3, 0.3, 1)
        _RimPower("Rim Power", Range(0, 20)) = 6

        [Header(Foliage)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0

        [Header(Bling Effect)]
        [Toggle(_BLING_WORLDSPACE_ON)] _BlingWorldSpace("Use World Space Bling", Float) = 0
        [HDR] _BlingColor("Bling Color", Color) = (1, 1, 1, 1)
        _BlingIntensity("Bling Intensity", Range(0, 10)) = 2.0
        _BlingScale("Bling Scale", Range(1, 10000)) = 50.0
        _BlingSpeed("Bling Speed", Range(0, 5)) = 1.0
        _BlingFresnelPower("Bling Fresnel Power", Range(0.1, 10)) = 2.0
        _BlingThreshold("Bling Threshold", Range(0.5, 1.0)) = 0.95

        [Header(Cosmic Surface)]
        _CosmicTex1("Nebula Layer 1 (R)", 2D) = "white" {}
        [HDR] _CosmicColor1("Layer 1 Color", Color) = (0.2, 0.5, 1.0, 1)
        _CosmicScale1("Layer 1 Scale", Float) = 1.0
        _CosmicScrollSpeed1("Layer 1 Scroll Speed", Float) = 0.05
        _CosmicParallaxDepth1("Layer 1 Parallax Depth", Range(-0.2, 0.2)) = 0.02
        _CosmicTex2("Nebula Layer 2 (R)", 2D) = "white" {}
        [HDR] _CosmicColor2("Layer 2 Color", Color) = (1.0, 0.3, 0.8, 1)
        _CosmicScale2("Layer 2 Scale", Float) = 1.5
        _CosmicScrollSpeed2("Layer 2 Scroll Speed", Float) = 0.02
        _CosmicParallaxDepth2("Layer 2 Parallax Depth", Range(-0.2, 0.2)) = 0.05
        _StarfieldTex("Star Field (R)", 2D) = "black" {}
        [HDR] _StarfieldColor("Stars Color", Color) = (1.0, 1.0, 0.8, 1)
        _StarfieldScale("Stars Scale", Float) = 2.5
        _StarfieldScrollSpeed("Stars Scroll Speed", Float) = 0.01
        _StarfieldParallaxDepth("Stars Parallax Depth", Range(-0.2, 0.2)) = 0.08
        _TriplanarSharpness("Triplanar Blend Sharpness", Range(1, 20)) = 5.0
        [HDR] _CosmicAmbientColor("Ambient Color", Color) = (0.1, 0.1, 0.2, 1)

        [Header(Outline)]
        [Enum(Off, 0, Fresnel, 1, Hull, 2)] _OutlineMode ("Mode", Float) = 0

        [Header(Fresnel Outline)]
        [HDR] _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 100.0)) = 5.0
        _FresnelOutlineSharpness("Sharpness", Range(0.1, 10.0)) = 2.0
        [Toggle(_OUTLINEGLINT_ON)] _GlintToggle("Enable Glint Effect", Float) = 0
        [HDR] _GlintColor("Glint Color", Color) = (1, 1, 0.5, 1)
        _GlintScale("Glint Scale", Float) = 20.0
        _GlintSpeed("Glint Speed", Range(0.1, 10.0)) = 2.0
        _GlintThreshold("Glint Threshold", Range(0.5, 0.99)) = 0.95

        [Header(Hull Outline)]
        _OutlineColor("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Width", Range(0.0, 10)) = 1.0
        [Toggle(_OUTLINE_SCALE_WITH_DISTANCE)] _OutlineScaleWithDistance("Screen-Space Scaling", Float) = 1
        _DistanceFadeStart("Distance Fade Start", Float) = 20
        _DistanceFadeEnd("Distance Fade End", Float) = 30

        [Header(Dissolve Control)]
        [Toggle(_DISSOLVE_ON)] _EnableDissolve ("Enable Dissolve", Float) = 0
        [Enum(Noise, 0, Linear, 1, Radial, 2, Pattern, 3, Alpha Blend, 4, Shatter, 5)] _DissolveType ("Dissolve Type", Float) = 0
        _DissolveThreshold ("Threshold (Standard)", Range(-10, 10)) = -0.5
        _RevealProgress ("Progress (Hologram)", Range(-10, 10)) = 0.0
        _RadialDirection ("Radial Direction", Range(-1, 1)) = 1
        [Toggle] _UseTimeAnimation ("Use Time Animation", Float) = 0
        _TimeScale ("Time Scale", Float) = 1
        [Toggle(_DISSOLVE_LOCALSPACE_ON)] _UseLocalSpace ("Use Local Space", Float) = 0
        _DissolveDirection ("Direction (Linear/Shatter)", Vector) = (0, 1, 0, 0)

        [Header(Edge Appearance)]
        _NoiseTex ("Noise Texture (R)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 0.1
        _DissolveEdgeWidth ("Edge Width", Range(0, 2)) = 0.05
        [HDR] _DissolveEdgeColor ("Edge Color (HDR)", Color) = (1, 0.5, 0, 1)
        _MaxDissolveDistance("Radius (Radial)", Float) = 1.0

        [Header(Pattern Settings)]
        [Enum(SinCos, 0, Checker, 1, Grid, 2)]_PatternType ("Pattern Type", Float) = 0
        _PatternFrequency ("Pattern Frequency", Float) = 10

        [Header(Alpha Blend Settings)]
        _AlphaFadeRange ("Fade Range", Range(0.01, 1)) = 0.5

        [Header(Vertex Effects)]
        [Toggle(_VERTEX_DISPLACEMENT_ON)] _EnableVertexDisplacement ("Enable Standard Displacement", Float) = 0
        [Toggle(_DISPLACEMENT_SATURATE_ON)] _UseSaturateDisplacement ("Saturate Displacement (Sustained)", Float) = 0
        [Toggle(_SHATTER_EFFECT_ON)] _EnableShatterEffect ("Enable Shatter Effect", Float) = 0
        _VertexDisplacement ("Intensity / Outward Push", Range(-5, 5)) = 0.1
        _BounceWaveWidth ("Effect Width", Range(0.01, 10)) = 0.5

        [Header(Shatter Effect)]
        _ShatterStrength ("Overall Strength", Range(0, 5)) = 1
        _ShatterLiftSpeed ("Lift Speed", Float) = 1
        _ShatterOffsetStrength ("Offset Strength", Float) = 0.5
        _ShatterTriggerRange ("Trigger Range", Range(0, 1)) = 0.1

        [Header(Hologram Reveal Effect)]
        [Toggle(_HOLOGRAM_REVEAL_ON)] _EnableHologramReveal ("Enable Hologram Reveal Mode", Float) = 0
        _HologramPatternTex ("Hologram Pattern (A)", 2D) = "white" {}
        [HDR] _HologramEmissionColor ("Hologram Emission", Color) = (0, 1, 1, 1)
        _HologramPatternScale ("Hologram Pattern Scale", Float) = 1.0
        _HologramFlickerSpeed ("Hologram Flicker Speed", Range(0, 100)) = 20

        [Header(Tiered Overlay Effects)]
        [Enum(None, 0, Rare Pulsing Glow, 1, Rare Sparkles, 2, Epic Fire Aura, 3, Epic Electric Field, 4, Legendary Cosmic Rift, 5, Legendary Holy Aura, 6)]
        _EffectType ("Effect Type", Float) = 0

        _EffectTex1 ("Effect Texture 1", 2D) = "white" {}

        [HDR] _RareColor1 ("Rare Color 1", Color) = (1, 1, 1, 1)
        _RareFloat1 ("Rare Float 1", Float) = 1
        _RareFloat2 ("Rare Float 2", Float) = 1

        [HDR] _EpicColor1 ("Epic Color 1 (RGBA)", Color) = (1, 0.5, 0, 1)
        [HDR] _EpicColor2 ("Epic Color 2", Color) = (1, 1, 0, 1)
        _EpicFloat1 ("Epic Float 1", Float) = 1
        _EpicFloat2 ("Epic Float 2", Float) = 1
        _EpicFloat3 ("Epic Float 3", Float) = 1

        [HDR] _LegendaryColor1 ("Legendary Color 1", Color) = (0.5, 0, 1, 1)
        [HDR] _LegendaryColor2 ("Legendary Color 2", Color) = (0, 1, 1, 1)
        _LegendaryFloat1 ("Legendary Float 1", Float) = 1
        _LegendaryFloat2 ("Legendary Float 2", Float) = 1
        _LegendaryFloat3 ("Legendary Float 3", Float) = 1

        [HideInInspector] _SurfaceType("Surface Type", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry"
        }

        Pass
        {
            Name "Outline"
            Cull Front

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag

            #pragma shader_feature_local _OUTLINEMODE_HULL
            #pragma shader_feature_local _OUTLINE_SCALE_WITH_DISTANCE

            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local _ _DISSOLVE_ON
            #pragma shader_feature_local _ _DISSOLVE_LOCALSPACE_ON
            #pragma shader_feature_local _ _VERTEX_DISPLACEMENT_ON
            #pragma shader_feature_local _ _SHATTER_EFFECT_ON
            #pragma shader_feature_local _ _DISPLACEMENT_SATURATE_ON
            #pragma multi_compile_local _DISSOLVETYPE_NOISE _DISSOLVETYPE_LINEAR _DISSOLVETYPE_RADIAL _DISSOLVETYPE_PATTERN _DISSOLVETYPE_ALPHA_BLEND _DISSOLVETYPE_SHATTER
            #pragma shader_feature_local_fragment _HOLOGRAM_REVEAL_ON

            #include "Includes/ToonUberCore_HQ.hlsl"

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float dissolveValue : TEXCOORD1;
            };

            OutlineVaryings OutlineVert(Attributes input)
            {
                OutlineVaryings output = (OutlineVaryings)0;
                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;

                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, input.color);
                #endif

                #if defined(_DISSOLVE_ON)
                    float perVertexNoise = MU_Hash11(positionOS.x + positionOS.y * 10.0h + positionOS.z * 100.0h);
                    float3 positionForDissolve = positionOS;
                    #ifndef _DISSOLVE_LOCALSPACE_ON
                        positionForDissolve = TransformObjectToWorld(positionOS);
                    #endif
                    output.dissolveValue = CalculateDissolveValue(positionForDissolve, input.uv, perVertexNoise);
                    positionOS = ApplyDissolveVertexEffects(positionOS, normalOS, output.dissolveValue, perVertexNoise);
                #else
                        output.dissolveValue = 2.0h;
                #endif

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float camDist = distance(TransformObjectToWorld(positionOS), _WorldSpaceCameraPos.xyz);
                float distFade = 1.0 - saturate((camDist - _DistanceFadeStart) / (_DistanceFadeEnd - _DistanceFadeStart + 0.00001));
                float scaledWidth = _OutlineWidth * 0.01 * distFade;

                #if defined(_OUTLINE_SCALE_WITH_DISTANCE)
                    float4 positionCS = TransformObjectToHClip(positionOS);
                    float3 normalWS = TransformObjectToWorldNormal(normalOS);
                    float3 normalVS = TransformWorldToViewDir(normalWS);

                    float2 screenSpaceNormal = normalize(mul((float2x3)UNITY_MATRIX_P, normalVS).xy);
                    positionCS.xy += screenSpaceNormal * scaledWidth * positionCS.w;

                    output.positionCS = positionCS;
                #else
                        positionOS += normalOS * scaledWidth;
                    output.positionCS = TransformObjectToHClip(positionOS);
                #endif

                return output;
            }

            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {
                if (_OutlineMode < 1.5h)
                {
                    clip(-1);
                }

                half4 finalColor = _OutlineColor;

                #if defined(_DISSOLVE_ON)
                    finalColor = ApplyDissolveFragmentEffect(finalColor, input.dissolveValue, input.uv, float3(0, 0, 0));
                    clip(finalColor.a - 0.5h);
                #endif

                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull [_CullMode]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _SURFACETYPE_OPAQUE _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE _SURFACETYPE_BLING _SURFACETYPE_COSMIC
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _OUTLINEGLINT_ON
            #pragma shader_feature_local_fragment _BLING_WORLDSPACE_ON

            #pragma shader_feature_local _ _DISSOLVE_ON
            #pragma shader_feature_local _ _DISSOLVE_LOCALSPACE_ON
            #pragma shader_feature_local _ _VERTEX_DISPLACEMENT_ON
            #pragma shader_feature_local _ _SHATTER_EFFECT_ON
            #pragma shader_feature_local _ _DISPLACEMENT_SATURATE_ON
            #pragma multi_compile_local _DISSOLVETYPE_NOISE _DISSOLVETYPE_LINEAR _DISSOLVETYPE_RADIAL _DISSOLVETYPE_PATTERN _DISSOLVETYPE_ALPHA_BLEND _DISSOLVETYPE_SHATTER
            #pragma shader_feature_local_fragment _HOLOGRAM_REVEAL_ON

            #pragma shader_feature_local_fragment _EFFECT_RARE_PULSING_GLOW
            #pragma shader_feature_local_fragment _EFFECT_RARE_SPARKLES
            #pragma shader_feature_local_fragment _EFFECT_EPIC_FIRE_AURA
            #pragma shader_feature_local_fragment _EFFECT_EPIC_ELECTRIC_FIELD
            #pragma shader_feature_local_fragment _EFFECT_LEGENDARY_COSMIC_RIFT
            #pragma shader_feature_local_fragment _EFFECT_LEGENDARY_HOLY_AURA

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Includes/ToonUberCore_HQ.hlsl"
            #include "Includes/ToonUber_TieredEffects_HQ.hlsl"

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                float3 positionOS = v.positionOS.xyz;

                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, v.color);
                #endif

                #if defined(_DISSOLVE_ON)
                    o.perVertexNoise = MU_Hash11(positionOS.x + positionOS.y * 10.0h + positionOS.z * 100.0h);
                    float3 positionForDissolve = positionOS;
                    #ifndef _DISSOLVE_LOCALSPACE_ON
                        positionForDissolve = TransformObjectToWorld(positionOS);
                    #endif
                    o.dissolveValue = CalculateDissolveValue(positionForDissolve, v.uv, o.perVertexNoise);
                    positionOS = ApplyDissolveVertexEffects(positionOS, v.normalOS, o.dissolveValue, o.perVertexNoise);
                #else
                        o.dissolveValue = 2.0h;
                #endif

                o.positionWS = TransformObjectToWorld(positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;

                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;

                return o;
            }

            half4 frag(Varyings i, half frontFace : VFACE) : SV_Target
            {
                ApplyAlphaClip(i.uv);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                float3 baseNormalWS = normalize(i.normalWS * sign(frontFace));
                float3 normalWS = ApplyNormalMap(i.uv, baseNormalWS, i.tangentWS, i.bitangentWS);

                Light mainLight = GetEffectiveMainLight(i.positionWS);

                half3 sceneAmbient = SampleSH(normalWS);
                half3 ambient;
                #if defined(_SURFACETYPE_COSMIC)
                    ambient = lerp(sceneAmbient, _CosmicAmbientColor.rgb, _CosmicAmbientColor.a);
                #else
                        ambient = lerp(sceneAmbient, _AmbientColor.rgb, _AmbientColor.a);
                #endif

                half3 lighting = 0;
                #if defined(_SURFACETYPE_OPAQUE)
                    lighting = CalculateToonLighting(normalWS, i.positionWS, mainLight);
                #elif defined(_SURFACETYPE_METALLIC)
                    lighting = CalculateMetallicLighting(normalWS, viewDir, mainLight);
                #elif defined(_SURFACETYPE_FOLIAGE)
                    lighting = CalculateFoliageLighting(normalWS, i.positionWS, mainLight);
                #elif defined(_SURFACETYPE_BLING)
                    lighting = CalculateBlingEffect(albedo.rgb, normalWS, i.positionWS, i.uv, mainLight, viewDir, i.positionCS);
                #elif defined(_SURFACETYPE_COSMIC)
                    lighting = CalculateCosmicLighting(normalWS, viewDir, i.positionWS, mainLight);
                #endif

                half3 surfaceColor;
                #if defined(_SURFACETYPE_BLING) || defined(_SURFACETYPE_COSMIC)
                    surfaceColor = lighting + ambient;
                #else
                        surfaceColor = albedo.rgb * (lighting + ambient);
                #endif

                surfaceColor = ApplyEmission(surfaceColor, i.uv);
                surfaceColor = ApplyFresnelOutline(surfaceColor, normalWS, viewDir, i.positionWS);
                surfaceColor += CalculateTieredEffect(i);

                half4 finalPixel = half4(surfaceColor, albedo.a);

                #if defined(_DISSOLVE_ON)
                    finalPixel = ApplyDissolveFragmentEffect(finalPixel, i.dissolveValue, i.uv, i.positionWS);
                #endif

                clip(finalPixel.a - 0.01h);

                return finalPixel;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma shader_feature_local _ _DISSOLVE_ON
            #pragma shader_feature_local _ _DISSOLVE_LOCALSPACE_ON
            #pragma multi_compile_local _DISSOLVETYPE_NOISE _DISSOLVETYPE_LINEAR _DISSOLVETYPE_RADIAL _DISSOLVETYPE_PATTERN _DISSOLVETYPE_ALPHA_BLEND _DISSOLVETYPE_SHATTER
            #pragma shader_feature_local_fragment _HOLOGRAM_REVEAL_ON

            #include "Includes/ToonUberCore_HQ.hlsl"

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float dissolveValue : TEXCOORD1;
            };

            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;
                float3 positionOS = input.positionOS.xyz;

                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, input.color);
                #endif

                #if defined(_DISSOLVE_ON)
                    float perVertexNoise = MU_Hash11(positionOS.x + positionOS.y * 10.0h + positionOS.z * 100.0h);
                    float3 positionForDissolve = positionOS;
                    #ifndef _DISSOLVE_LOCALSPACE_ON
                        positionForDissolve = TransformObjectToWorld(positionOS);
                    #endif
                    o.dissolveValue = CalculateDissolveValue(positionForDissolve, input.uv, perVertexNoise);
                #else
                        o.dissolveValue = 2.0h;
                #endif

                o.positionCS = GetShadowCoord(GetVertexPositionInputs(positionOS));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                ApplyAlphaClip(i.uv);

                #if defined(_DISSOLVE_ON)
                    half4 finalPixel = ApplyDissolveFragmentEffect(half4(0, 0, 0, 1), i.dissolveValue, i.uv, float3(0, 0, 0));
                    clip(finalPixel.a - 0.5h);
                #endif

                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonOpaqueShaderGUI_HQ"
}
