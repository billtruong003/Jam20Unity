Shader "TextMeshPro/Mobile/Sci-Fi SDF Ultimate V3 Configurable"
{
    Properties
    {
        [Header(Face and Outline Properties)]
        [HDR] _FaceColor("Face Color", Color) = (1, 1, 1, 1)
        _FaceDilate("Face Dilate", Range(-1, 1)) = 0
        [HDR] _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Thickness", Range(0, 1)) = 0
        _OutlineSoftness("Outline Softness", Range(0, 1)) = 0
        [HDR] _GlowColor("Global Glow", Color) = (0, 1, 1, 0)

        [Header(Master Effect Selection)]
        [KeywordEnum(None, SciFi, Fire, Water, Ice)] _EffectMode("Effect Mode", Float) = 0
        [HDR] _EffectTintColor("Effect Tint Color", Color) = (1, 1, 1, 1)

        [Header(SciFi Effects)]
        [Toggle(_GLITCH_ON)] _GlitchToggle("Enable Glitch", Float) = 0
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _GlitchStrength("Glitch Tear Strength", Range(0, 0.5)) = 0.1
        _GlitchSpeed("Glitch Speed", Range(0, 20)) = 10

        [Toggle(_SCAN_LINES_ON)] _ScanLinesToggle("Enable Scan Lines", Float) = 0
        _ScanLinesDensity("Scan Lines Density", Range(0, 1000)) = 300
        _ScanLinesSpeed("Scan Lines Speed", Range(-10, 10)) = -2
        _ScanLinesIntensity("Scan Lines Intensity", Range(0, 1)) = 0.5

        [Toggle(_CHROMATIC_ABERRATION_ON)] _ChromaticToggle("Enable Chromatic Aberration", Float) = 0
        _ChromaticAberrationAmount("Chromatic Aberration", Range(0, 0.02)) = 0.002

        [Toggle(_HOLO_GRID_ON)] _HoloGridToggle("Enable Holo Grid", Float) = 0
        [HDR] _HoloGridColor("Holo Grid Color", Color) = (0, 1, 1, 0.5)
        _HoloGridTiling("Holo Grid Tiling", Float) = 15.0
        _HoloGridSpeed("Holo Grid Speed", Float) = 2.0

        [Header(Fire Effect)]
        _FireGradient("Fire Dissolve Gradient", 2D) = "white" {}
        _FireSpeed("Fire Speed", Float) = 0.5
        _FireTurbulence("Fire Turbulence", Range(0, 0.2)) = 0.05
        _FireGlowIntensity("Fire Glow Intensity", Range(0, 10)) = 2.0

        [Header(Water Effect)]
        _WaterSpeed("Water Speed", Float) = 0.5
        _WaterDistortion("Water Distortion", Range(0, 0.1)) = 0.02
        _CausticsTex("Caustics Texture", 2D) = "white" {}
        _CausticsTiling("Caustics Tiling", Float) = 4.0
        _CausticsIntensity("Caustics Intensity", Range(0, 2)) = 1.0

        [Header(Ice Effect)]
        _IceCrystalTex("Ice Crystal (Voronoi)", 2D) = "white" {}
        _IceRefraction("Inner Refraction", Range(0, 0.1)) = 0.015
        _FrostAmount("Frost Amount", Range(0, 1)) = 0.25

        [Header(Advanced Rendering Options)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0
        [Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Cull Mode", Float) = 0

        // TMP Properties
        _WeightNormal("Weight Normal", float) = 0
        _WeightBold("Weight Bold", float) = 0.5
        _ShaderFlags("Flags", float) = 0
        _ScaleRatioA("Scale RatioA", float) = 1
        _ScaleRatioB("Scale RatioB", float) = 1
        _ScaleRatioC("Scale RatioC", float) = 1
        _MainTex("Font Atlas", 2D) = "white" {}
        _TextureWidth("Texture Width", float) = 512
        _TextureHeight("Texture Height", float) = 512
        _GradientScale("Gradient Scale", float) = 5
        _ScaleX("Scale X", float) = 1
        _ScaleY("Scale Y", float) = 1
        _PerspectiveFilter("Perspective Correction", Range(0, 1)) = 0.875
        _Sharpness("Sharpness", Range(-1, 1)) = 0
        _VertexOffsetX("Vertex OffsetX", float) = 0
        _VertexOffsetY("Vertex OffsetY", float) = 0
        _ClipRect("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
        _MaskSoftnessX("Mask SoftnessX", float) = 0
        _MaskSoftnessY("Mask SoftnessY", float) = 0
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"
        }
        Stencil
        {
            Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite [_ZWrite]
        ZTest [_ZTest]

        Lighting Off
        Fog
        {
            Mode Off
        }
        Blend One OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex VertShader
            #pragma fragment PixShader

            #pragma shader_feature_local_fragment _EFFECTMODE_SCIFI
            #pragma shader_feature_local_fragment _EFFECTMODE_FIRE
            #pragma shader_feature_local_fragment _EFFECTMODE_WATER
            #pragma shader_feature_local_fragment _EFFECTMODE_ICE

            #pragma shader_feature_local_fragment _GLITCH_ON
            #pragma shader_feature_local_fragment _SCAN_LINES_ON
            #pragma shader_feature_local_fragment _CHROMATIC_ABERRATION_ON
            #pragma shader_feature_local_fragment _HOLO_GRID_ON
            #pragma shader_feature_local_fragment OUTLINE_ON

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // The rest of the CGPROGRAM is identical to the previous optimized version.
            // No changes are needed here.

            struct VertexInput
            {UNITY_VERTEX_INPUT_INSTANCE_ID float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float4 texcoord0 : TEXCOORD0;
            };
            struct PixelInput
            {UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO float4 vertex : SV_POSITION;
                fixed4 faceColor : COLOR;
                fixed4 outlineColor : COLOR1;
                float4 texcoord0 : TEXCOORD0;
                half4 param : TEXCOORD1;
                half4 mask : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };
            sampler2D _MainTex, _NoiseTex, _FireGradient, _CausticsTex, _IceCrystalTex;
            fixed4 _FaceColor, _OutlineColor, _GlowColor, _EffectTintColor, _HoloGridColor;
            float _FaceDilate, _OutlineWidth, _OutlineSoftness;
            float _WeightNormal, _WeightBold, _ScaleRatioA;
            float _GradientScale, _ScaleX, _ScaleY, _PerspectiveFilter, _Sharpness;
            float _VertexOffsetX, _VertexOffsetY;
            float4 _ClipRect;
            float _MaskSoftnessX, _MaskSoftnessY;
            float _GlitchStrength, _GlitchSpeed;
            float _ScanLinesDensity, _ScanLinesSpeed, _ScanLinesIntensity;
            float _ChromaticAberrationAmount;
            float _HoloGridTiling, _HoloGridSpeed;
            float _FireSpeed, _FireTurbulence, _FireGlowIntensity;
            float _WaterSpeed, _WaterDistortion;
            float _CausticsTiling, _CausticsIntensity;
            float _IceRefraction, _FrostAmount;
            float fbm(float2 uv)
            {float value = 0.;
                float amplitude=.5;
                for(int i = 0;
                i < 4;
                i++)
                {
                    value += amplitude * tex2D(_NoiseTex, uv).r;
                    uv *= 2.;
                    amplitude*=.5;
                }
                return value;
            }
            PixelInput VertShader(VertexInput input)
            {PixelInput output;
                UNITY_INITIALIZE_OUTPUT(PixelInput, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float bold = step(input.texcoord0.w, 0);
                float4 vert = input.vertex;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;
                float4 vPosition = UnityObjectToClipPos(vert);
                float2 pixelSize = vPosition.w / (abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy)) * float2(_ScaleX, _ScaleY));
                float scale = rsqrt(dot(pixelSize, pixelSize)) * abs(input.texcoord0.w) * _GradientScale * (_Sharpness + 1);
                if (UNITY_MATRIX_P[3][3]==0)scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));
                    float weight = (lerp(_WeightNormal, _WeightBold, bold) / 4.+_FaceDilate) * _ScaleRatioA*.5;
                scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
                float bias = (.5 - weight) * scale-.5;
                float outline = _OutlineWidth * _ScaleRatioA*.5 * scale;
                fixed4 faceColor = input.color * _FaceColor;
                faceColor.rgb *= faceColor.a;
                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= input.color.a;
                outlineColor.rgb *= outlineColor.a;
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                output.vertex = vPosition;
                output.faceColor = faceColor;
                output.outlineColor = lerp(faceColor, outlineColor, saturate(outline * 2.));
                output.texcoord0 = float4(input.texcoord0.xy, maskUV.xy);
                output.param = half4(scale, bias - outline, bias + outline, bias);
                output.mask = half4(vert.xy * 2-clampedRect.xy - clampedRect.zw, .25 / (.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
                output.screenPos = ComputeScreenPos(vPosition);
                return output;
            }
            half SampleSDF(float2 uv, float scale)
            {return tex2D(_MainTex, uv).a * scale;
            }
            fixed4 PixShader(PixelInput input) : SV_Target
            {UNITY_SETUP_INSTANCE_ID(input);
                float2 initialAtlasUV = input.texcoord0.xy;
                float2 remapUV = input.texcoord0.zw;
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                half sdf_scale = input.param.x;
                half sdf_bias = input.param.w;
                float2 baseAtlasUV = initialAtlasUV;
                float2 total_ca_offset = float2(0, 0);
                #if _EFFECTMODE_SCIFI
                    #if _GLITCH_ON
                        float glitch_time = _Time.y * _GlitchSpeed;
                        float intensity_noise_val = fbm(float2(glitch_time*.2, initialAtlasUV.y * 2.));
                        float glitch_intensity = pow(intensity_noise_val, 4.);
                        float tear_noise_val = tex2D(_NoiseTex, float2(initialAtlasUV.x*.5, glitch_time*.5)).r;
                        float horizontal_tear = (tear_noise_val-.5) * _GlitchStrength * 2.*glitch_intensity;
                        float jitter_noise_val = tex2D(_NoiseTex, float2(glitch_time * 2., initialAtlasUV.y * 10.)).g;
                        float vertical_jitter = (jitter_noise_val-.5) * _GlitchStrength*.2 * glitch_intensity;
                        baseAtlasUV += float2(horizontal_tear, vertical_jitter);
                        total_ca_offset.x += glitch_intensity * _GlitchStrength*.5;
                    #endif
                    #if _CHROMATIC_ABERRATION_ON
                        total_ca_offset.x += _ChromaticAberrationAmount;
                    #endif
                #endif
                #if _EFFECTMODE_WATER
                    float2 water_motion = float2(_Time.y * _WaterSpeed*.1, _Time.y * _WaterSpeed* -.05);
                    baseAtlasUV += (fbm((baseAtlasUV * 3.) + water_motion) -.5) * _WaterDistortion;
                #endif
                #if _EFFECTMODE_ICE
                    float2 parallax_uv = screenUV * 4.+_Time.y*.1;
                    baseAtlasUV += (tex2D(_IceCrystalTex, parallax_uv).r-.5) * _IceRefraction;
                #endif
                half sdfR = SampleSDF(baseAtlasUV + total_ca_offset, sdf_scale);
                half sdfG = SampleSDF(baseAtlasUV, sdf_scale);
                half sdfB = SampleSDF(baseAtlasUV - total_ca_offset, sdf_scale);
                half sdf_max = max(sdfR, max(sdfG, sdfB));
                fixed3 face_color_channels = fixed3(saturate(sdfR - sdf_bias), saturate(sdfG - sdf_bias), saturate(sdfB - sdf_bias));
                fixed4 finalColor = input.faceColor;
                finalColor.rgb *= face_color_channels;
                finalColor.a *= saturate(sdf_max - sdf_bias);
                #if OUTLINE_ON
                    half outline_blend = saturate(sdf_max - input.param.z);
                    finalColor = lerp(input.outlineColor, finalColor, outline_blend);
                    half antialias_mask = saturate(sdf_max - input.param.y);
                    finalColor.a = lerp(input.outlineColor.a, input.faceColor.a, outline_blend) * antialias_mask;
                #endif
                #if _EFFECTMODE_FIRE
                    float turbulence = (fbm(baseAtlasUV * 4.+_Time.y * _FireSpeed) -.5) * _FireTurbulence;
                    float dissolve_value = frac(remapUV.y * 1.5 - _Time.y * _FireSpeed*.3 + turbulence);
                    fixed3 fire_gradient_color = tex2D(_FireGradient, float2(dissolve_value, .5)).rgb;
                    fixed3 fire_color = fire_gradient_color * _EffectTintColor.rgb;
                    finalColor.rgb = lerp(finalColor.rgb, fire_color, finalColor.a);
                    finalColor.rgb += fire_color * pow(dissolve_value, 2) * _FireGlowIntensity * finalColor.a;
                    finalColor.a *= saturate(1.-dissolve_value * 1.1);
                #endif
                #if _EFFECTMODE_WATER
                    finalColor.rgb = lerp(finalColor.rgb, _EffectTintColor.rgb, finalColor.a);
                    float2 caustic_uv = screenUV * _CausticsTiling + _Time.y*.15;
                    float caustics = tex2D(_CausticsTex, caustic_uv).r * tex2D(_CausticsTex, caustic_uv*.7+.5).r;
                    finalColor.rgb += caustics * _CausticsIntensity * _EffectTintColor.rgb * finalColor.a;
                #endif
                #if _EFFECTMODE_ICE
                    finalColor.rgb = lerp(finalColor.rgb, _EffectTintColor.rgb, finalColor.a);
                    float frost_noise = fbm(screenUV * 15.);
                    finalColor.rgb = lerp(finalColor.rgb, _EffectTintColor.rgb * 1.5, frost_noise * _FrostAmount * finalColor.a);
                #endif
                finalColor.rgb += _GlowColor.rgb * finalColor.a * _GlowColor.a;
                #if _EFFECTMODE_SCIFI
                    #if _SCAN_LINES_ON
                        float scanLineWave = sin(screenUV.y * _ScanLinesDensity + _Time.y * _ScanLinesSpeed);
                        finalColor.rgb *= lerp(1., 0., _ScanLinesIntensity * (1.-scanLineWave) *.5);
                    #endif
                    #if _HOLO_GRID_ON
                        float2 grid_uv = screenUV * _HoloGridTiling;
                        float grid_speed = _Time.y * _HoloGridSpeed;
                        float line_x = pow(abs(frac(grid_uv.x + grid_speed) -.5), .1);
                        float line_y = pow(abs(frac(grid_uv.y) -.5), .1);
                        float grid_value = (1.-saturate(min(line_x, line_y)));
                        finalColor.rgb += grid_value * _HoloGridColor.rgb * _HoloGridColor.a * finalColor.a;
                    #endif
                #endif
                #if UNITY_UI_CLIP_RECT
                    half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                    finalColor.a *= m.x * m.y;
                #endif
                #if UNITY_UI_ALPHACLIP
                    clip(finalColor.a-.001);
                #endif
                return finalColor;
            }

            ENDCG
        }
    }
    CustomEditor "SciFiShaderUltimateGUI_V3_Configurable"
}
