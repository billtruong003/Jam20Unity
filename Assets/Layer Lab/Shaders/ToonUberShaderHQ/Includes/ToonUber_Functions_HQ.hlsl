#ifndef BILLS_TOON_FUNCTIONS_HQ_INCLUDED
#define BILLS_TOON_FUNCTIONS_HQ_INCLUDED

#include "../../Others/MathUtils.hlsl"

#if defined(_OUTLINEGLINT_ON)
float CalculateGlintFactor(float3 worldPos)
{
    float noiseTime = _Time.y * _GlintSpeed;
    float2 noiseUV = worldPos.xy * _GlintScale * 0.1;
    noiseUV.y += noiseTime;

    float noise = MU_SimplexNoise(noiseUV) * 0.5 + 0.5;
    float glint = smoothstep(_GlintThreshold, _GlintThreshold + 0.05, noise);
    
    return glint;
}
#endif

half TriplanarSample(TEXTURE2D_PARAM(tex, samp), float3 position, float3 normal, float scale, float sharpness)
{
    float2 uvX = position.yz * scale;
    float2 uvY = position.xz * scale;
    float2 uvZ = position.xy * scale;

    half sampleX = SAMPLE_TEXTURE2D_LOD(tex, samp, uvX, 0).r;
    half sampleY = SAMPLE_TEXTURE2D_LOD(tex, samp, uvY, 0).r;
    half sampleZ = SAMPLE_TEXTURE2D_LOD(tex, samp, uvZ, 0).r;

    float3 blendWeights = pow(abs(normal), sharpness);
    blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

    return sampleX * blendWeights.x + sampleY * blendWeights.y + sampleZ * blendWeights.z;
}

float3 ApplyThreeStepToonRamp(float NdotL, float3 lightColor)
{
    half shadowRamp = smoothstep(_ShadowThreshold - _RampSmoothness, _ShadowThreshold + _RampSmoothness, NdotL);
    half3 colorAfterShadow = lerp(_ShadowTint.rgb, _MidtoneColor.rgb, shadowRamp);

    half midtoneRamp = smoothstep(_MidtoneThreshold - _RampSmoothness, _MidtoneThreshold + _RampSmoothness, NdotL);
    half3 finalColor = lerp(colorAfterShadow, lightColor, midtoneRamp);

    return finalColor;
}

float3 ApplyAdditionalLightToonRamp(float NdotL, float3 lightColor)
{
    half shadowRamp = smoothstep(_AddLightShadowThreshold - _AddLightRampSmoothness, _AddLightShadowThreshold + _AddLightRampSmoothness, NdotL);
    half3 colorAfterShadow = lerp(_AddLightShadowTint.rgb, _AddLightMidtoneColor.rgb, shadowRamp);

    half midtoneRamp = smoothstep(_AddLightMidtoneThreshold - _AddLightRampSmoothness, _AddLightMidtoneThreshold + _AddLightRampSmoothness, NdotL);
    half3 finalColor = lerp(colorAfterShadow, lightColor, midtoneRamp);

    return finalColor;
}

float3 CalculateToonLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 mainLightContribution = ApplyThreeStepToonRamp(NdotL, mainLight.color) * mainLight.shadowAttenuation;

    float3 additionalLightContribution = 0.0h;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            additionalLightContribution += ApplyAdditionalLightToonRamp(addNdotL, additionalLight.color) * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif

    return mainLightContribution + additionalLightContribution;
}

float3 CalculateStylizedSurfaceLighting(float3 normalWS, float3 viewDir, Light mainLight)
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

    return lighting;
}

float3 CalculateMetallicLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    return CalculateStylizedSurfaceLighting(normalWS, viewDir, mainLight);
}

float3 CalculateCosmicLighting(float3 normalWS, float3 viewDir, float3 positionWS, Light mainLight)
{
    float3 surfaceLighting = CalculateStylizedSurfaceLighting(normalWS, viewDir, mainLight);

    float3 parallaxOffset1 = viewDir.xyz * _CosmicParallaxDepth1;
    float3 parallaxOffset2 = viewDir.xyz * _CosmicParallaxDepth2;
    float3 parallaxOffset3 = viewDir.xyz * _StarfieldParallaxDepth;

    float3 pos1 = positionWS + parallaxOffset1 + _Time.y * _CosmicScrollSpeed1;
    float3 pos2 = positionWS + parallaxOffset2 + _Time.y * _CosmicScrollSpeed2;
    float3 pos3 = positionWS + parallaxOffset3 + _Time.y * _StarfieldScrollSpeed;
    
    half cosmicLayer1 = TriplanarSample(TEXTURE2D_ARGS(_CosmicTex1, sampler_CosmicTex1), pos1, normalWS, _CosmicScale1, _TriplanarSharpness);
    half cosmicLayer2 = TriplanarSample(TEXTURE2D_ARGS(_CosmicTex2, sampler_CosmicTex2), pos2, normalWS, _CosmicScale2, _TriplanarSharpness);
    half starfieldLayer = TriplanarSample(TEXTURE2D_ARGS(_StarfieldTex, sampler_StarfieldTex), pos3, normalWS, _StarfieldScale, _TriplanarSharpness);

    half3 cosmicColor = cosmicLayer1 * _CosmicColor1.rgb + cosmicLayer2 * _CosmicColor2.rgb + starfieldLayer * _StarfieldColor.rgb;

    return surfaceLighting + cosmicColor;
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

    return totalLight;
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

#if defined(_DISSOLVE_ON)

float CalculateDissolveValue(float3 position, float2 uv, float perVertexNoise)
{
    float dissolveValue = 0.0h;

    #if defined(_DISSOLVETYPE_NOISE)
        dissolveValue = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv * _NoiseScale, 0).r;
    #elif defined(_DISSOLVETYPE_LINEAR)
        dissolveValue = dot(position, normalize(_DissolveDirection.xyz));
    #elif defined(_DISSOLVETYPE_RADIAL)
        half dist = distance(position, _DissolveDirection.xyz);
        half gradient = saturate(dist / max(_MaxDissolveDistance, 0.001h));
        dissolveValue = (_RadialDirection < 0.0) ? (1.0h - gradient) : gradient;
    #elif defined(_DISSOLVETYPE_PATTERN)
        if (_PatternType == 0)
        {
            dissolveValue = (sin(position.x * _PatternFrequency) * cos(position.y * _PatternFrequency) + 1.0h) * 0.5h;
        }
        else if (_PatternType == 1)
        {
            float2 grid = floor(position.xy * _PatternFrequency);
            dissolveValue = fmod(grid.x + grid.y, 2.0h);
        }
        else
        {
            float2 gridUV = frac(position.xy * _PatternFrequency);
            float lineThickness = 0.1h;
            float2 lines = step(1.0h - lineThickness, gridUV);
            dissolveValue = 1.0h - max(lines.x, lines.y);
        }
    #elif defined(_DISSOLVETYPE_SHATTER)
        dissolveValue = perVertexNoise;
    #endif

    return dissolveValue;
}

float3 ApplyShatterEffect(float3 positionOS, float threshold, float perturbedDissolveValue, float perVertexNoise)
{
    #if defined(_SHATTER_EFFECT_ON)
        float shatterProgress = saturate(MU_Remap(perturbedDissolveValue, threshold - _ShatterTriggerRange, threshold, 1.0h, 0.0h));
        float lift = pow(shatterProgress, 2.0h) * _ShatterLiftSpeed;
        float3 direction = normalize(_DissolveDirection.xyz);
        float3 offset = (MU_Hash22(positionOS.xy).xy - 0.5h).xyy * _ShatterOffsetStrength;
        
        positionOS += direction * lift * _ShatterStrength;
        positionOS += offset * shatterProgress;
        positionOS += perVertexNoise * _VertexDisplacement * shatterProgress;
    #endif
    return positionOS;
}

float3 ApplyStandardVertexDisplacement(float3 positionOS, float3 normalOS, float threshold, float perturbedDissolveValue)
{
    #if defined(_VERTEX_DISPLACEMENT_ON)
        float displacementFactor = 1.0h - smoothstep(threshold - _BounceWaveWidth, threshold, perturbedDissolveValue);
        #if defined(_DISPLACEMENT_SATURATE_ON)
            displacementFactor = saturate(displacementFactor);
        #endif
        positionOS.xyz += normalOS * displacementFactor * _VertexDisplacement;
    #endif
    return positionOS;
}

float3 ApplyDissolveVertexEffects(float3 positionOS, float3 normalOS, float dissolveValue, float perVertexNoise)
{
    #if defined(_VERTEX_DISPLACEMENT_ON) || defined(_SHATTER_EFFECT_ON)
        half timeAnimOffset = _UseTimeAnimation > 0.5h ? sin(_Time.y * _TimeScale) * 0.05h : 0.0h;
        half threshold;
        #if defined(_HOLOGRAM_REVEAL_ON)
            threshold = 1.0h - saturate(_RevealProgress * 0.5h);
        #else
            threshold = _DissolveThreshold;
        #endif
        threshold += timeAnimOffset;
        
        half vertexPerturbation = (perVertexNoise - 0.5h) * _NoiseStrength;
        half perturbedDissolveValueForVertex = dissolveValue + vertexPerturbation;

        #if defined(_DISSOLVETYPE_SHATTER)
            positionOS = ApplyShatterEffect(positionOS, threshold, perturbedDissolveValueForVertex, perVertexNoise);
        #else
            positionOS = ApplyStandardVertexDisplacement(positionOS, normalOS, threshold, perturbedDissolveValueForVertex);
        #endif
    #endif
    return positionOS;
}

half4 GetHologramPixelData(float3 worldPos)
{
    float2 patternUV = worldPos.xy * _HologramPatternScale * 0.1h;
    patternUV.y += _Time.y * 0.1h;
    float pattern = SAMPLE_TEXTURE2D(_HologramPatternTex, sampler_HologramPatternTex, patternUV).a;
    
    float flicker = (sin(_Time.y * _HologramFlickerSpeed) * 0.5h + 0.5h) * 0.3h + 0.7h;
    half3 emission = _HologramEmissionColor.rgb * flicker * pattern;
    
    return half4(emission, pattern);
}

half4 CalculateHologramRevealEffect(half4 currentPixel, half perturbedDissolveValue, float3 worldPos, half timeAnimOffset)
{
    half progress = _RevealProgress + timeAnimOffset;
    
    half hologramReveal = saturate(progress);
    half materialReveal = saturate(progress - 1.0h);
    
    half hologramThreshold = 1.0h - hologramReveal;
    half materialThreshold = 1.0h - materialReveal;
    
    half hologramVisibility = smoothstep(hologramThreshold - _DissolveEdgeWidth, hologramThreshold, perturbedDissolveValue);
    half materialVisibility = smoothstep(materialThreshold - _DissolveEdgeWidth, materialThreshold, perturbedDissolveValue);
    
    half hologramEdgeFactor = hologramVisibility - smoothstep(hologramThreshold, hologramThreshold + 0.001h, perturbedDissolveValue);
    half materialEdgeFactor = materialVisibility - smoothstep(materialThreshold, materialThreshold + 0.001h, perturbedDissolveValue);
    
    half4 hologramPixel = GetHologramPixelData(worldPos);
    hologramPixel.rgb = lerp(hologramPixel.rgb, _DissolveEdgeColor.rgb, hologramEdgeFactor);
    
    currentPixel.rgb = lerp(currentPixel.rgb, _DissolveEdgeColor.rgb, materialEdgeFactor);
    
    half4 finalPixel;
    finalPixel.rgb = lerp(hologramPixel.rgb, currentPixel.rgb, materialVisibility);
    finalPixel.a = lerp(hologramPixel.a, currentPixel.a, materialVisibility) * hologramVisibility;
    
    return finalPixel;
}

half4 CalculateStandardDissolveEffect(half4 currentPixel, half perturbedDissolveValue, half timeAnimOffset)
{
    half threshold = _DissolveThreshold + timeAnimOffset;
    
    #if defined(_DISSOLVETYPE_ALPHA_BLEND)
        half remappedAlpha = 1.0h - saturate((perturbedDissolveValue - threshold) / max(_AlphaFadeRange, 0.001h));
        currentPixel.a *= remappedAlpha;
    #else
        half edgeStart = threshold - _DissolveEdgeWidth;
        half dissolveAlpha = smoothstep(edgeStart, threshold, perturbedDissolveValue);
        half edgeColorFactor = dissolveAlpha - smoothstep(threshold, threshold + 0.001h, perturbedDissolveValue);
        currentPixel.rgb = lerp(currentPixel.rgb, _DissolveEdgeColor.rgb, edgeColorFactor);
        currentPixel.a *= dissolveAlpha;
    #endif
    
    return currentPixel;
}

half4 ApplyDissolveFragmentEffect(half4 currentPixel, float baseDissolveValue, float2 uv, float3 worldPos)
{
    half timeAnimOffset = _UseTimeAnimation > 0.5h ? sin(_Time.y * _TimeScale) * 0.05h : 0.0h;
    half perturbedDissolveValue = baseDissolveValue;

    #if !defined(_DISSOLVETYPE_NOISE)
        half noiseTexSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * _NoiseScale).r;
        half pixelPerturbation = (noiseTexSample - 0.5h) * _NoiseStrength;
        perturbedDissolveValue += pixelPerturbation;
    #endif
    
    #if defined(_HOLOGRAM_REVEAL_ON)
        return CalculateHologramRevealEffect(currentPixel, perturbedDissolveValue, worldPos, timeAnimOffset);
    #else
        return CalculateStandardDissolveEffect(currentPixel, perturbedDissolveValue, timeAnimOffset);
    #endif
}

#endif

#endif