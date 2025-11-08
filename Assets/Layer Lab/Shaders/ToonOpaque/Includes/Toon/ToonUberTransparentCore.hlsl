#ifndef TOON_UBER_TRANSPARENT_CORE_INCLUDED
#define TOON_UBER_TRANSPARENT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);
TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;
    half4 _FakeLightColor;
    float4 _FakeLightDirection;
    half4 _GlassColor;
    half4 _FresnelColor;
    half _FresnelPower;
    half _RefractionStrength;
    half _GlassSpecularPower;
    half _GlassSpecularIntensity;
    half4 _FresnelOutlineColor;
    half _FresnelOutlineWidth;
    half _FresnelOutlinePower;
    half _FresnelOutlineSharpness;
    half4 _GlintColor;
    half _GlintScale;
    half _GlintSpeed;
    half _GlintThreshold;
CBUFFER_END

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    half4 color         : COLOR;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    half4 color         : TEXCOORD3;
    float4 screenPos    : TEXCOORD4;
};

half3 ApplyGlintEffect(half3 surfaceColor, half3 normalWS, half3 viewDir, Light light, float2 positionCS)
{
#if _OUTLINEGLINT_ON
    half3 halfDir = SafeNormalize(light.direction + viewDir);
    half spec = pow(saturate(dot(normalWS, halfDir)), _GlassSpecularPower);

    float2 motion = float2(sin(_Time.y * _GlintSpeed), cos(_Time.y * _GlintSpeed * 0.75));
    float2 glintUV = positionCS.xy * _GlintScale + motion;
    
    half noise = (sin(glintUV.x) + cos(glintUV.y)) * 0.5;
    
    if (noise > _GlintThreshold && spec > 0.0)
    {
        surfaceColor += _GlintColor.rgb * spec;
    }
#endif
    return surfaceColor;
}

half3 ApplyFresnelOutline(half3 surfaceColor, half3 normalWS, half3 viewDir, float3 positionWS)
{
#if _OUTLINEMODE_FRESNEL
    half fresnel = 1.0 - saturate(dot(normalWS, viewDir));
    fresnel = pow(fresnel, _FresnelOutlinePower);
    half edge = smoothstep(1.0 - _FresnelOutlineWidth, 1.0 - _FresnelOutlineWidth + _FresnelOutlineSharpness, fresnel);
    surfaceColor = lerp(surfaceColor, _FresnelOutlineColor.rgb, edge * _FresnelOutlineColor.a);
#endif
    return surfaceColor;
}

half3 ApplyEmission(half3 surfaceColor, float2 uv)
{
#if _EMISSION_ON
    half3 emissionSample = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;
    surfaceColor += emissionSample * _EmissionColor.rgb;
#endif
    return surfaceColor;
}

half3 CalculateGlassLighting(Varyings i, Light mainLight, half3 viewDir, half3 ambient)
{
    half3 normalWS = normalize(i.normalWS);
    
    float2 screenUV = i.screenPos.xy / i.screenPos.w;
    float2 refractionUVOffset = normalWS.xy * _RefractionStrength;
    half3 refractedBG = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + refractionUVOffset).rgb;

    half3 lightDir = mainLight.direction;
    half3 lightColor = mainLight.color;

#if _FAKELIGHT_ON
    lightDir = normalize(_FakeLightDirection.xyz);
    lightColor = _FakeLightColor.rgb;
#endif
    
    half NdotL = saturate(dot(normalWS, lightDir));
    half3 diffuse = (NdotL * lightColor + ambient) * _GlassColor.rgb;

    half3 halfDir = SafeNormalize(lightDir + viewDir);
    half specTerm = pow(saturate(dot(normalWS, halfDir)), _GlassSpecularPower);
    half3 specular = specTerm * lightColor * _GlassSpecularIntensity;

    half fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), _FresnelPower);
    half3 fresnelColor = fresnel * _FresnelColor.rgb * _FresnelColor.a;

    half3 finalColor = lerp(refractedBG, diffuse, _GlassColor.a);
    finalColor += specular;
    finalColor += fresnelColor;
    
    return finalColor;
}

#endif