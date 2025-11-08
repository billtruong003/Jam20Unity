#ifndef MINIONS_ART_SNOW_FUNCTIONS_INCLUDED
#define MINIONS_ART_SNOW_FUNCTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _SnowColor, _ShadowColor, _RimColor, _PathColorIn, _PathColorOut;
    float _RimPower, _SnowHeight, _SnowTextureOpacity, _SnowTextureScale, _NoiseScale;
    float _NoiseWeight, _TessellationFactor, _MaxTessellationDistance, _PathBlending;
    float _SnowPathStrength, _SparkleScale, _SparkCutoff, _SnowNormalStrength;
CBUFFER_END

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_NoiseTexture); SAMPLER(sampler_NoiseTexture);
TEXTURE2D(_SparkleNoise); SAMPLER(sampler_SparkleNoise);
TEXTURE2D(_Normal); SAMPLER(sampler_Normal);

TEXTURE2D(_GlobalEffectRT); SAMPLER(sampler_GlobalEffectRT);
float3 _InteractorPosition;
float _OrthographicCamSize;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float3 viewDirWS    : TEXCOORD3;
    float  effectStrength: TEXCOORD4;
    float3 tangentWS    : TEXCOORD5;
    float3 bitangentWS  : TEXCOORD6;
    float  fogFactor    : TEXCOORD7;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

float CalculateDistanceTessFactor(float3 positionWS, float maxDistance, float tessFactor)
{
    float distanceToCamera = distance(positionWS, _WorldSpaceCameraPos);
    float fade = saturate(1.0 - (distanceToCamera / maxDistance));
    return lerp(1, tessFactor, fade);
}

Varyings Vertex(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
    
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
    OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;
    OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
    OUT.viewDirWS = normalize(_WorldSpaceCameraPos - OUT.positionWS);
    OUT.uv = IN.uv;
    OUT.effectStrength = 0;
    OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);

    return OUT;
}

[domain("tri")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[patchconstantfunc("HullConstant")]
[outputcontrolpoints(3)]
Varyings Hull(InputPatch<Varyings, 3> patch, uint id : SV_OutputControlPointID)
{
    return patch[id];
}

TessellationFactors HullConstant(InputPatch<Varyings, 3> patch)
{
    float tessFactor = _TessellationFactor;
    float maxDistance = _MaxTessellationDistance;

    float3 edgeFactors;
    edgeFactors.x = CalculateDistanceTessFactor(0.5 * (patch[1].positionWS + patch[2].positionWS), maxDistance, tessFactor);
    edgeFactors.y = CalculateDistanceTessFactor(0.5 * (patch[0].positionWS + patch[2].positionWS), maxDistance, tessFactor);
    edgeFactors.z = CalculateDistanceTessFactor(0.5 * (patch[0].positionWS + patch[1].positionWS), maxDistance, tessFactor);
    
    TessellationFactors OUT;
    OUT.edge[0] = edgeFactors.x;
    OUT.edge[1] = edgeFactors.y;
    OUT.edge[2] = edgeFactors.z;
    OUT.inside = (edgeFactors.x + edgeFactors.y + edgeFactors.z) / 3.0;

    return OUT;
}

void InterpolateVaryings(out Varyings output, const OutputPatch<Varyings, 3> patch, float3 barycentricCoords)
{
    ZERO_INITIALIZE(Varyings, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    #define INTERPOLATE(name) output.name = patch[0].name * barycentricCoords.x + patch[1].name * barycentricCoords.y + patch[2].name * barycentricCoords.z;

    INTERPOLATE(positionWS);
    INTERPOLATE(normalWS);
    INTERPOLATE(tangentWS);
    INTERPOLATE(bitangentWS);
    INTERPOLATE(uv);
    
    #undef INTERPOLATE

    output.normalWS = normalize(output.normalWS);
    output.tangentWS = normalize(output.tangentWS);
    output.bitangentWS = normalize(output.bitangentWS);
    output.viewDirWS = normalize(_WorldSpaceCameraPos - output.positionWS);
}

void ApplyVertexDisplacement(inout Varyings data)
{
    float2 rtUV = (data.positionWS.xz - _InteractorPosition.xz) / (_OrthographicCamSize * 2.0) + 0.5;
    
    float effectSample = SAMPLE_TEXTURE2D_LOD(_GlobalEffectRT, sampler_GlobalEffectRT, rtUV, 0).g;
    effectSample *= smoothstep(0.0, 0.1, rtUV.x) * smoothstep(1.0, 0.9, rtUV.x);
    effectSample *= smoothstep(0.0, 0.1, rtUV.y) * smoothstep(1.0, 0.9, rtUV.y);
    data.effectStrength = effectSample;

    float snowNoise = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture, data.positionWS.xz * _NoiseScale, 0).r;
    float heightDisplacement = (_SnowHeight + (snowNoise * _NoiseWeight)) * (1.0 - saturate(data.effectStrength * _SnowPathStrength));

    data.positionWS += data.normalWS * heightDisplacement;
    data.positionCS = TransformWorldToHClip(data.positionWS);
    data.fogFactor = ComputeFogFactor(data.positionCS.z);
}

float4 CalculateFragmentColor(Varyings IN)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    float3 T = normalize(IN.tangentWS);
    float3 B = normalize(IN.bitangentWS);
    float3 N = normalize(IN.normalWS);
    float3x3 TBN = float3x3(T, B, N);
    
    float3 normalSample = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, IN.positionWS.xz * _SnowTextureScale));
    float3 detailedNormalWS = lerp(N, mul(normalSample, TBN), _SnowNormalStrength);

    float3 snowTexture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.positionWS.zx * _SnowTextureScale).rgb;
    float3 mainColors = lerp(_SnowColor.rgb, snowTexture * _SnowColor.rgb, _SnowTextureOpacity);

    float saturatedEffect = saturate(IN.effectStrength);
    float3 path = lerp(_PathColorOut.rgb, _PathColorIn.rgb, saturate(saturatedEffect * _PathBlending));
    float3 albedo = lerp(mainColors, path, saturatedEffect);

    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
    
    float3 additionalLights = 0;
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, IN.positionWS);
        float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        additionalLights += LightingLambert(attenuatedLightColor, light.direction, detailedNormalWS);
    }
    
    float rim = pow(1.0 - saturate(dot(IN.viewDirWS, detailedNormalWS)), _RimPower);
    rim = lerp(rim, 0, saturatedEffect);
    float3 coloredRim = _RimColor.rgb * rim;
    
    float sparkleSample = SAMPLE_TEXTURE2D(_SparkleNoise, sampler_SparkleNoise, IN.positionWS.xz * _SparkleScale).r;
    float sparkleCutoff = step(_SparkCutoff, sparkleSample);
    float3 sparkles = lerp(sparkleCutoff * _SnowColor.rgb * 2.0, 0, saturatedEffect * 2.0);
    
    float3 emission = coloredRim + sparkles;

    float NdotL = saturate(dot(detailedNormalWS, mainLight.direction));
    float3 attenuatedLightColor = mainLight.color * mainLight.shadowAttenuation;
    float3 mainLightLighting = lerp(_ShadowColor.rgb, attenuatedLightColor, NdotL);
    
    float3 ambient = SampleSH(detailedNormalWS);

    float3 finalColor = albedo * (mainLightLighting + ambient + additionalLights) + emission;
    finalColor = MixFog(finalColor, IN.fogFactor);
    
    return float4(finalColor, 1.0);
}

#endif