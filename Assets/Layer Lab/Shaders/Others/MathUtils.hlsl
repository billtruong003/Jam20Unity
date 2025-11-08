// File: MathUtils_Core.hlsl
// Purpose: A pure, dependency-free math library for HLSL.
// Safe to include in any shader pass.

#ifndef MATH_UTILS_CORE_INCLUDED
#define MATH_UTILS_CORE_INCLUDED

// --- CONSTANTS ---
#define MU_PI                  3.14159265359f
#define MU_TWO_PI              6.28318530718f
#define MU_FOUR_PI             12.5663706144f
#define MU_PI_HALF             1.57079632679f
#define MU_PI_QUARTER          0.78539816339f

#define MU_INV_PI              0.31830988618f
#define MU_INV_TWO_PI          0.15915494309f
#define MU_INV_FOUR_PI         0.07957747154f

#define MU_SQRT2               1.41421356237f
#define MU_INV_SQRT2           0.70710678118f

#define MU_Epsilon             1e-6f
#define MU_HalfEpsilon         1e-4h

// --- HASHING ---
float MU_Hash11(float p)
{
    p = frac(p * 0.1031f);
    p *= p + 33.33f;
    p *= p + p;
    return frac(p);
}

float2 MU_Hash12(float p)
{
    float3 p3 = frac(float3(p, p, p) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float MU_Hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031f);
    p3 += dot(p3, p3.yzx + 33.33f);
    return frac((p3.x + p3.y) * p3.z);
}

float2 MU_Hash22(float2 p)
{
	float3 p3 = frac(float3(p.xyx) * float3(0.1031f, 0.1030f, 0.0973f));
	p3 += dot(p3, p3.yzx + 33.33f);
	return frac((p3.xx + p3.yz) * p3.zy);
}

float MU_Hash31(float3 p)
{
    p  = frac(p * 0.1031f);
    p += dot(p, p.yzx + 33.33f);
    return frac((p.x + p.y) * p.z);
}

// --- FAST MATH APPROXIMATIONS ---
half MU_FastPow(half base, half power)
{
    return exp2(log2(saturate(base)) * power);
}

#define MU_FAST_SIN_PARAM_A 1.27323954474h
#define MU_FAST_SIN_PARAM_B 0.40528473456h

half MU_FastSin(half x)
{
    x = x * MU_INV_TWO_PI;
    x = frac(x) * 2.0h - 1.0h;
    return MU_FAST_SIN_PARAM_A * x - MU_FAST_SIN_PARAM_B * x * abs(x);
}

half MU_FastCos(half x)
{
    return MU_FastSin(x + MU_PI_HALF);
}

half MU_FastAtan2(half y, half x)
{
    half ax = abs(x);
    half ay = abs(y);
    half a = min(ax, ay) / (max(ax, ay) + MU_HalfEpsilon);
    half s = a * a;
    half r = ((-0.0464964749f * s + 0.15931422f) * s - 0.327622764f) * s * a + a;
    r = (ax < ay) ? MU_PI_HALF - r : r;
    r = (x < 0) ? MU_PI - r : r;
    r = (y < 0) ? -r : r;
    return r;
}

// --- EASING FUNCTIONS ---
half MU_EaseInSine(half t) { return 1.0h - cos(t * MU_PI_HALF); }
half MU_EaseOutSine(half t) { return sin(t * MU_PI_HALF); }
half MU_EaseInOutSine(half t) { return -(cos(MU_PI * t) - 1.0h) * 0.5h; }

half MU_EaseInQuad(half t) { return t * t; }
half MU_EaseOutQuad(half t) { return 1.0h - (1.0h - t) * (1.0h - t); }
half MU_EaseInOutQuad(half t) { return t < 0.5h ? 2.0h * t * t : 1.0h - pow(-2.0h * t + 2.0h, 2.0h) * 0.5h; }

// --- INTERPOLATION ---
half MU_FastSmoothstep(half a, half b, half x)
{
    half t = saturate((x - a) / (b - a + MU_Epsilon));
    return t * t * (3.0h - 2.0h * t);
}

half MU_SmootherStep(half a, half b, half x)
{
    half t = saturate((x - a) / (b - a + MU_Epsilon));
    return t * t * t * (t * (t * 6.0h - 15.0h) + 10.0h);
}

half MU_Remap(half value, half inMin, half inMax, half outMin, half outMax)
{
    half t = saturate((value - inMin) / (inMax - inMin + MU_Epsilon));
    return lerp(outMin, outMax, t);
}

// --- VECTOR MATH ---
float2 MU_RotateVector2D(float2 v, float angle)
{
    float s, c;
    sincos(angle, s, c);
    return float2(c * v.x - s * v.y, s * v.x + c * v.y);
}

float3 MU_ProjectVector(float3 a, float3 b)
{
    float3 bNormalized = normalize(b);
    return dot(a, bNormalized) * bNormalized;
}

// --- COORDINATE SYSTEMS ---
float2 MU_CartesianToPolar(float2 cartesian)
{
    return float2(length(cartesian), MU_FastAtan2(cartesian.y, cartesian.x));
}

float2 MU_PolarToCartesian(float2 polar)
{
    float s, c;
    sincos(polar.y, s, c);
    return float2(polar.x * c, polar.x * s);
}

// --- NOISE & PATTERNS ---
half MU_Impulse(half k, half x)
{
    half h = k * x;
    return h * exp(1.0h - h);
}

float4 MU_TaylorInvSqrt(float4 r) { return 1.79284291400159f - 0.85373472095314f * r; }
float3 MU_TaylorInvSqrt(float3 r) { return 1.79284291400159f - 0.85373472095314f * r; }
float4 MU_Permute(float4 x) { return fmod(((x * 34.0f) + 1.0f) * x, 289.0f); }
float3 MU_Permute(float3 x) { return fmod(((x * 34.0f) + 1.0f) * x, 289.0f); }

float MU_SimplexNoise(float2 v)
{
    const float4 C = float4(0.211324865405187f, 0.366025403784439f, -0.577350269189626f, 0.024390243902439f);
    float2 i  = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);
    float2 i1 = (x0.x > x0.y) ? float2(1.0f, 0.0f) : float2(0.0f, 1.0f);
    float4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = fmod(i, 289.0f);
    float3 p = MU_Permute(MU_Permute(i.y + float3(0.0f, i1.y, 1.0f)) + i.x + float3(0.0f, i1.x, 1.0f));
    float3 m = max(0.5f - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0f);
    m = m * m;
    m = m * m;
    float3 x = 2.0f * frac(p * C.w) - 1.0f;
    float3 h = abs(x) - 0.5f;
    float3 ox = floor(x + 0.5f);
    float3 a0 = x - ox;
    m *= MU_TaylorInvSqrt(a0 * a0 + h * h);
    float3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0f * dot(m, g);
}

#endif