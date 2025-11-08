#ifndef BILLS_TOON_FUNCTIONS_INCLUDED
#define BILLS_TOON_FUNCTIONS_INCLUDED

#include "../../../Others/MathUtils.hlsl"

#if defined(_OUTLINEGLINT_ON)
float CalculateGlintFactor(float3 worldPos)
{
    float2 noiseUV = worldPos.xy * _GlintScale * 0.1;
    
    half spatialNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
    
    float timeStep = floor(_Time.y * _GlintSpeed * 10.0);
    half temporalNoise = MU_Hash31(float3(worldPos.xy * 0.1, timeStep));
    
    half combinedNoise = spatialNoise * temporalNoise;

    float glint = smoothstep(_GlintThreshold, _GlintThreshold + 0.05, combinedNoise);
    
    return glint;
}
#endif

float3 ApplyConfigurableToonRamp(float NdotL, float3 lightColor, float3 shadowTint, float3 midtoneColor, float shadowThreshold, float midtoneThreshold, float smoothness)
{
    half3 rampedLight;
    #if defined(_TOON_STYLE_HARD)
        half shadowFactor = step(shadowThreshold, NdotL);
        half midtoneFactor = step(midtoneThreshold, NdotL);
        rampedLight = lerp(shadowTint, midtoneColor, shadowFactor);
        rampedLight = lerp(rampedLight, lightColor, midtoneFactor);
    #else
        half shadowFactor = MU_FastSmoothstep(shadowThreshold, shadowThreshold + smoothness, NdotL);
        half midtoneFactor = MU_FastSmoothstep(midtoneThreshold, midtoneThreshold + smoothness, NdotL);
        rampedLight = lerp(shadowTint, midtoneColor, shadowFactor);
        rampedLight = lerp(rampedLight, lightColor, midtoneFactor);
    #endif
    return rampedLight;
}

float3 CalculateToonLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 mainLightRamp = ApplyConfigurableToonRamp(NdotL, mainLight.color, _ShadowTint.rgb, _MidtoneColor.rgb, _ShadowThreshold, _MidtoneThreshold, _ToonRampSmoothness);
    float3 mainLightContribution = mainLightRamp * mainLight.shadowAttenuation;

    float3 additionalLightContribution = 0.0h;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            float3 addLightRamp = ApplyConfigurableToonRamp(addNdotL, additionalLight.color, _AddLightShadowTint.rgb, _AddLightMidtoneColor.rgb, _AddLightShadowThreshold, _AddLightMidtoneThreshold, _AddLightRampSmoothness);
            additionalLightContribution += addLightRamp * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif
    
    float3 totalLighting = mainLightContribution + additionalLightContribution;
    return min(totalLighting, _MaxBrightness);
}

float3 CalculateMetallicLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));
    float NdotL = saturate(dot(normalWS, mainLight.direction));
    float NdotV = saturate(dot(normalWS, viewDir));

    half3 rampColor = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(NdotL, 0.5h)).rgb;
    half specularRamp = MU_FastSmoothstep(_Offset, _Offset + 0.05, NdotH);
    half highlightRamp = MU_FastSmoothstep(_HighlightOffset, _HighlightOffset + 0.05, NdotH);

    half3 specular = specularRamp * _SpecuColor.rgb;
    half3 highlight = highlightRamp * _HiColor.rgb;
    float3 rim = MU_FastPow(1.0h - NdotV, _RimPower) * _RimColor.rgb;

    float3 lighting = (rampColor + specular + highlight) * _Brightness * mainLight.color * mainLight.shadowAttenuation;
    lighting += rim;

    return min(lighting, _MaxBrightness);
}

float3 CalculateFoliageLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 lambert = mainLight.color * NdotL;

    float3 backLightDir = -mainLight.direction;
    float backNdotL = dot(normalWS, backLightDir) * 0.5 + 0.5;
    float3 translucency = pow(backNdotL, 2) * mainLight.color * _TranslucencyStrength * _TranslucencyColor;
    float3 totalLight = (lambert + translucency) * mainLight.shadowAttenuation;

    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            totalLight += additionalLight.color * addNdotL * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif

    return min(totalLight, _MaxBrightness);
}

void ApplyWind(inout float3 positionOS, float4 vertexColor)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    float windPhase = dot(worldPos.xz, float2(0.2, 0.1));
    float windSine = MU_FastSin(_Time.y * _WindFrequency + windPhase);
    float3 windVector = normalize(_WindDirection) * windSine * _WindAmplitude;
    float windMask = vertexColor.a;
    positionOS.xyz += windVector * windMask;
}

#endif