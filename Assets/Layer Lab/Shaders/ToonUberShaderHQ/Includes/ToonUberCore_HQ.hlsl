#ifndef BILLS_TOON_CORE_HQ_INCLUDED
#define BILLS_TOON_CORE_HQ_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

struct Attributes {
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
    float4 tangentOS    : TANGENT;
};

struct Varyings {
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    float4 color        : COLOR;
    float  dissolveValue: TEXCOORD4;
    float  perVertexNoise:TEXCOORD5;
    float3 tangentWS    : TEXCOORD6;
    float3 bitangentWS  : TEXCOORD7;
};

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseColor;
float  _BumpScale;
float  _Cutoff;

float4 _EmissionColor;

float4 _FakeLightColor;
float3 _FakeLightDirection;

float4 _ShadowTint;
float4 _MidtoneColor;
float  _ShadowThreshold;
float  _MidtoneThreshold;
float  _RampSmoothness;
float4 _AmbientColor;

float4 _AddLightShadowTint;
float4 _AddLightMidtoneColor;
float  _AddLightShadowThreshold;
float  _AddLightMidtoneThreshold;
float  _AddLightRampSmoothness;

float  _Brightness;
float  _Offset;
float  _HighlightOffset;
float  _RimPower;
float4 _SpecuColor;
float4 _HiColor;
float4 _RimColor;

float  _WindFrequency;
float  _WindAmplitude;
float3 _WindDirection;
float3 _TranslucencyColor;
float  _TranslucencyStrength;

float4 _BlingColor;
float  _BlingIntensity;
float  _BlingScale;
float  _BlingSpeed;
float  _BlingThreshold;
float  _BlingFresnelPower;

float4 _CosmicColor1;
float  _CosmicScale1;
float  _CosmicScrollSpeed1;
float  _CosmicParallaxDepth1;
float4 _CosmicColor2;
float  _CosmicScale2;
float  _CosmicScrollSpeed2;
float  _CosmicParallaxDepth2;
float4 _StarfieldColor;
float  _StarfieldScale;
float  _StarfieldScrollSpeed;
float  _StarfieldParallaxDepth;
float  _TriplanarSharpness;
float4 _CosmicAmbientColor;

float4 _FresnelOutlineColor;
float  _FresnelOutlineWidth;
float  _FresnelOutlinePower;
float  _FresnelOutlineSharpness;

float4 _GlintColor;
float  _GlintScale;
float  _GlintSpeed;
float  _GlintThreshold;

float4 _OutlineColor;
float  _OutlineWidth;
float  _DistanceFadeStart;
float  _DistanceFadeEnd;

float  _OutlineMode; 
int    _DissolveType;
float  _DissolveThreshold;
float  _RevealProgress;
float  _RadialDirection;
float  _UseTimeAnimation;
float  _TimeScale;
float4 _DissolveDirection;
float  _NoiseScale;
float  _NoiseStrength;
float  _DissolveEdgeWidth;
float4 _DissolveEdgeColor;
float  _MaxDissolveDistance;
int    _PatternType;
float  _PatternFrequency;
float  _AlphaFadeRange;
float  _VertexDisplacement;
float  _BounceWaveWidth;
float  _ShatterStrength;
float  _ShatterLiftSpeed;
float  _ShatterOffsetStrength;
float  _ShatterTriggerRange;
float4 _HologramEmissionColor;
float  _HologramPatternScale;
float  _HologramFlickerSpeed;

float4 _RareColor1;
float  _RareFloat1;
float  _RareFloat2;
float4 _EpicColor1;
float4 _EpicColor2;
float  _EpicFloat1;
float  _EpicFloat2;
float  _EpicFloat3;
float4 _LegendaryColor1;
float4 _LegendaryColor2;
float  _LegendaryFloat1;
float  _LegendaryFloat2;
float  _LegendaryFloat3;

CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_Ramp);           SAMPLER(sampler_Ramp);
TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex);
TEXTURE2D(_HologramPatternTex); SAMPLER(sampler_HologramPatternTex);
TEXTURE2D(_CosmicTex1);     SAMPLER(sampler_CosmicTex1);
TEXTURE2D(_CosmicTex2);     SAMPLER(sampler_CosmicTex2);
TEXTURE2D(_StarfieldTex);   SAMPLER(sampler_StarfieldTex);
TEXTURE2D(_EffectTex1);     SAMPLER(sampler_EffectTex1);

#include "Includes/ToonUber_Functions_HQ.hlsl"

float3 ApplyNormalMap(float2 uv, float3 normalWS, float3 tangentWS, float3 bitangentWS) {
    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
    float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
    return normalize(mul(normalTS, TBN));
}

void ApplyAlphaClip(float2 uv) {
#if defined(_ALPHACLIP_ON)
    half albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
    clip(albedoAlpha - _Cutoff);
#endif
}

half3 ApplyEmission(half3 surfaceColor, float2 uv) {
#if defined(_EMISSION_ON)
    surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
    return surfaceColor;
}

half3 ApplyFresnelOutline(half3 surfaceColor, float3 normalWS, float3 viewDir, float3 worldPos) {
#if defined(_OUTLINEMODE_FRESNEL)
    float fresnelDot = dot(normalWS, viewDir);
    float fresnelTerm = 1.0 - saturate(fresnelDot);
    float fresnelPower = MU_FastPow(fresnelTerm, _FresnelOutlinePower);

    float screenSpaceDerivative = fwidth(fresnelPower);
    float edgeWidth = screenSpaceDerivative * _FresnelOutlineSharpness;

    float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth - edgeWidth, 1.0 - _FresnelOutlineWidth, fresnelPower);

    half3 finalOutlineColor = _FresnelOutlineColor.rgb;

#if defined(_OUTLINEGLINT_ON)
    float glintFactor = CalculateGlintFactor(worldPos);
    finalOutlineColor = lerp(finalOutlineColor, _GlintColor.rgb, glintFactor);
#endif

    surfaceColor = lerp(surfaceColor, finalOutlineColor, outlineFactor);
#endif
    return surfaceColor;
}

Light GetEffectiveMainLight(float3 positionWS) {
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));

#if defined(_FAKELIGHT_ON)
    bool hasRealLight = dot(mainLight.color, mainLight.color) > 0.001;
    if (!hasRealLight) {
        mainLight.direction = normalize(_FakeLightDirection.xyz);
        mainLight.color = _FakeLightColor.rgb;
        mainLight.shadowAttenuation = 1.0;
    }
#endif
    return mainLight;
}

float3 CalculateBlingEffect(float3 baseColor, float3 normalWS, float3 worldPos, float2 uv, Light mainLight, float3 viewDirWS, float4 positionCS) {
    float3 baseLighting = CalculateToonLighting(normalWS, worldPos, mainLight);
    float3 shadedColor = baseColor * baseLighting;

    float2 noiseUV;
#if defined(_BLING_WORLDSPACE_ON)
    noiseUV = worldPos.xy * _BlingScale * 0.1h;
#else
    float2 screenPos = GetNormalizedScreenSpaceUV(positionCS);
    noiseUV = screenPos.xy * _BlingScale;
    noiseUV.x *= _ScreenParams.x / _ScreenParams.y;
#endif

    noiseUV.y += _Time.y * _BlingSpeed;

    half NdotV = 1.0h - saturate(dot(normalWS, viewDirWS));
    half fresnel = pow(NdotV, _BlingFresnelPower);

    half noise = MU_SimplexNoise(noiseUV);
    half sparkle = smoothstep(_BlingThreshold, _BlingThreshold + 0.05h, noise);

    half3 bling = sparkle * fresnel * _BlingColor.rgb * _BlingIntensity;

    return shadedColor + bling;
}
#endif