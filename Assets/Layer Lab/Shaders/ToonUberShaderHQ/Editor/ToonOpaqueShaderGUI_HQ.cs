using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;

public class ToonOpaqueShaderGUI_HQ : ToonUberShaderGUIBase
{
    private static bool showBaseSettings = true;
    private static bool showRenderStates = true;
    private static bool showLightingSettings = true;
    private static bool showOutlineSettings = true;
    private static bool showDissolveSettings = false;
    private static bool showTieredEffectSettings = true;
    private static bool showToonSettings = true;
    private static bool showMetallicSettings = true;
    private static bool showFoliageSettings = true;
    private static bool showBlingSettings = true;
    private static bool showCosmicSettings = true;

    private enum OutlineMode { Off, Fresnel, Hull }
    private enum RenderMode { Opaque, Cutout, Transparent }

    #region Property Structs
    private struct BaseProps
    {
        public MaterialProperty surfaceType;
        public MaterialProperty renderMode, srcBlend, dstBlend, zWrite, zTest, cullMode;
        public MaterialProperty baseMap, baseColor, bumpMap, bumpScale;
        public MaterialProperty cutoff;
        public MaterialProperty emissionMode, emissionColor, emissionMap;

        public void Find(MaterialProperty[] props)
        {
            surfaceType = FindProperty("_SurfaceType", props);
            renderMode = FindProperty("_RenderMode", props);
            srcBlend = FindProperty("_SrcBlend", props);
            dstBlend = FindProperty("_DstBlend", props);
            zWrite = FindProperty("_ZWrite", props);
            zTest = FindProperty("_ZTest", props);
            cullMode = FindProperty("_CullMode", props);
            baseMap = FindProperty("_BaseMap", props);
            baseColor = FindProperty("_BaseColor", props);
            bumpMap = FindProperty("_BumpMap", props);
            bumpScale = FindProperty("_BumpScale", props);
            cutoff = FindProperty("_Cutoff", props);
            emissionMode = FindProperty("_EmissionMode", props);
            emissionColor = FindProperty("_EmissionColor", props);
            emissionMap = FindProperty("_EmissionMap", props);
        }
    }

    private struct LightingProps
    {
        public MaterialProperty fakeLightMode, fakeLightColor, fakeLightDirection;
        public MaterialProperty ambientColor;

        public void Find(MaterialProperty[] props)
        {
            fakeLightMode = FindProperty("_FakeLightMode", props);
            fakeLightColor = FindProperty("_FakeLightColor", props);
            fakeLightDirection = FindProperty("_FakeLightDirection", props);
            ambientColor = FindProperty("_AmbientColor", props);
        }
    }

    public struct ToonShadingProps
    {
        public MaterialProperty shadowTint, midtoneColor, shadowThreshold, midtoneThreshold, rampSmoothness;
        public MaterialProperty addLightShadowTint, addLightMidtoneColor, addLightShadowThreshold, addLightMidtoneThreshold, addLightRampSmoothness;

        public void Find(MaterialProperty[] props)
        {
            shadowTint = FindProperty("_ShadowTint", props);
            midtoneColor = FindProperty("_MidtoneColor", props);
            shadowThreshold = FindProperty("_ShadowThreshold", props);
            midtoneThreshold = FindProperty("_MidtoneThreshold", props);
            rampSmoothness = FindProperty("_RampSmoothness", props);

            addLightShadowTint = FindProperty("_AddLightShadowTint", props);
            addLightMidtoneColor = FindProperty("_AddLightMidtoneColor", props);
            addLightShadowThreshold = FindProperty("_AddLightShadowThreshold", props);
            addLightMidtoneThreshold = FindProperty("_AddLightMidtoneThreshold", props);
            addLightRampSmoothness = FindProperty("_AddLightRampSmoothness", props);
        }
    }

    public struct MetallicProps
    {
        public MaterialProperty ramp, brightness, offset, specColor, hiOffset, hiColor, rimColor, rimPower;
        public void Find(MaterialProperty[] props)
        {
            ramp = FindProperty("_Ramp", props);
            brightness = FindProperty("_Brightness", props);
            offset = FindProperty("_Offset", props);
            specColor = FindProperty("_SpecuColor", props);
            hiOffset = FindProperty("_HighlightOffset", props);
            hiColor = FindProperty("_HiColor", props);
            rimColor = FindProperty("_RimColor", props);
            rimPower = FindProperty("_RimPower", props);
        }
    }

    public struct FoliageProps
    {
        public MaterialProperty windFreq, windAmp, windDir, transColor, transStrength;
        public void Find(MaterialProperty[] props)
        {
            windFreq = FindProperty("_WindFrequency", props);
            windAmp = FindProperty("_WindAmplitude", props);
            windDir = FindProperty("_WindDirection", props);
            transColor = FindProperty("_TranslucencyColor", props);
            transStrength = FindProperty("_TranslucencyStrength", props);
        }
    }

    public struct BlingProps
    {
        public MaterialProperty worldSpace, color, intensity, scale, speed, fresnelPower, threshold;
        public void Find(MaterialProperty[] props)
        {
            worldSpace = FindProperty("_BlingWorldSpace", props);
            color = FindProperty("_BlingColor", props);
            intensity = FindProperty("_BlingIntensity", props);
            scale = FindProperty("_BlingScale", props);
            speed = FindProperty("_BlingSpeed", props);
            fresnelPower = FindProperty("_BlingFresnelPower", props);
            threshold = FindProperty("_BlingThreshold", props);
        }
    }

    public struct CosmicProps
    {
        public MaterialProperty tex1, color1, scale1, speed1, parallax1;
        public MaterialProperty tex2, color2, scale2, speed2, parallax2;
        public MaterialProperty starTex, starColor, starScale, starSpeed, starParallax;
        public MaterialProperty triplanarSharpness, ambientColor;
        public void Find(MaterialProperty[] props)
        {
            tex1 = FindProperty("_CosmicTex1", props);
            color1 = FindProperty("_CosmicColor1", props);
            scale1 = FindProperty("_CosmicScale1", props);
            speed1 = FindProperty("_CosmicScrollSpeed1", props);
            parallax1 = FindProperty("_CosmicParallaxDepth1", props);
            tex2 = FindProperty("_CosmicTex2", props);
            color2 = FindProperty("_CosmicColor2", props);
            scale2 = FindProperty("_CosmicScale2", props);
            speed2 = FindProperty("_CosmicScrollSpeed2", props);
            parallax2 = FindProperty("_CosmicParallaxDepth2", props);
            starTex = FindProperty("_StarfieldTex", props);
            starColor = FindProperty("_StarfieldColor", props);
            starScale = FindProperty("_StarfieldScale", props);
            starSpeed = FindProperty("_StarfieldScrollSpeed", props);
            starParallax = FindProperty("_StarfieldParallaxDepth", props);
            triplanarSharpness = FindProperty("_TriplanarSharpness", props);
            ambientColor = FindProperty("_CosmicAmbientColor", props);
        }
    }

    private struct OutlineProps
    {
        public MaterialProperty outlineMode;
        public MaterialProperty fresnelColor, fresnelWidth, fresnelPower, fresnelSharpness;
        public MaterialProperty glintToggle, glintColor, glintScale, glintSpeed, glintThreshold;
        public MaterialProperty hullColor, hullWidth, hullScaleWithDistance, hullFadeStart, hullFadeEnd;

        public void Find(MaterialProperty[] props)
        {
            outlineMode = FindProperty("_OutlineMode", props);
            fresnelColor = FindProperty("_FresnelOutlineColor", props);
            fresnelWidth = FindProperty("_FresnelOutlineWidth", props);
            fresnelPower = FindProperty("_FresnelOutlinePower", props);
            fresnelSharpness = FindProperty("_FresnelOutlineSharpness", props);
            glintToggle = FindProperty("_GlintToggle", props);
            glintColor = FindProperty("_GlintColor", props);
            glintScale = FindProperty("_GlintScale", props);
            glintSpeed = FindProperty("_GlintSpeed", props);
            glintThreshold = FindProperty("_GlintThreshold", props);
            hullColor = FindProperty("_OutlineColor", props);
            hullWidth = FindProperty("_OutlineWidth", props);
            hullScaleWithDistance = FindProperty("_OutlineScaleWithDistance", props);
            hullFadeStart = FindProperty("_DistanceFadeStart", props);
            hullFadeEnd = FindProperty("_DistanceFadeEnd", props);
        }
    }

    public struct DissolveProps
    {
        public MaterialProperty enableDissolve, dissolveType, dissolveThreshold, revealProgress, useTimeAnimation, timeScale, useLocalSpace, dissolveDirection, radialDirection;
        public MaterialProperty noiseTex, noiseScale, noiseStrength, dissolveEdgeWidth, dissolveEdgeColor, maxDissolveDistance;
        public MaterialProperty patternType, patternFrequency, alphaFadeRange;
        public MaterialProperty enableVertexDisplacement, useSaturateDisplacement, enableShatterEffect, vertexDisplacement, bounceWaveWidth;
        public MaterialProperty shatterStrength, shatterLiftSpeed, shatterOffsetStrength, shatterTriggerRange;
        public MaterialProperty enableHologramReveal, hologramPatternTex, hologramEmissionColor, hologramPatternScale, hologramFlickerSpeed;

        public void Find(MaterialProperty[] props)
        {
            enableDissolve = FindProperty("_EnableDissolve", props);
            dissolveType = FindProperty("_DissolveType", props);
            dissolveThreshold = FindProperty("_DissolveThreshold", props);
            revealProgress = FindProperty("_RevealProgress", props);
            radialDirection = FindProperty("_RadialDirection", props);
            useTimeAnimation = FindProperty("_UseTimeAnimation", props);
            timeScale = FindProperty("_TimeScale", props);
            useLocalSpace = FindProperty("_UseLocalSpace", props);
            dissolveDirection = FindProperty("_DissolveDirection", props);
            noiseTex = FindProperty("_NoiseTex", props);
            noiseScale = FindProperty("_NoiseScale", props);
            noiseStrength = FindProperty("_NoiseStrength", props);
            dissolveEdgeWidth = FindProperty("_DissolveEdgeWidth", props);
            dissolveEdgeColor = FindProperty("_DissolveEdgeColor", props);
            maxDissolveDistance = FindProperty("_MaxDissolveDistance", props);
            patternType = FindProperty("_PatternType", props);
            patternFrequency = FindProperty("_PatternFrequency", props);
            alphaFadeRange = FindProperty("_AlphaFadeRange", props);
            enableVertexDisplacement = FindProperty("_EnableVertexDisplacement", props);
            useSaturateDisplacement = FindProperty("_UseSaturateDisplacement", props);
            enableShatterEffect = FindProperty("_EnableShatterEffect", props);
            vertexDisplacement = FindProperty("_VertexDisplacement", props);
            bounceWaveWidth = FindProperty("_BounceWaveWidth", props);
            shatterStrength = FindProperty("_ShatterStrength", props);
            shatterLiftSpeed = FindProperty("_ShatterLiftSpeed", props);
            shatterOffsetStrength = FindProperty("_ShatterOffsetStrength", props);
            shatterTriggerRange = FindProperty("_ShatterTriggerRange", props);
            enableHologramReveal = FindProperty("_EnableHologramReveal", props);
            hologramPatternTex = FindProperty("_HologramPatternTex", props);
            hologramEmissionColor = FindProperty("_HologramEmissionColor", props);
            hologramPatternScale = FindProperty("_HologramPatternScale", props);
            hologramFlickerSpeed = FindProperty("_HologramFlickerSpeed", props);
        }
    }

    public struct TieredEffectProps
    {
        public MaterialProperty effectType;
        public MaterialProperty effectTex1;
        public MaterialProperty rareColor1, rareFloat1, rareFloat2;
        public MaterialProperty epicColor1, epicColor2, epicFloat1, epicFloat2, epicFloat3;
        public MaterialProperty legendaryColor1, legendaryColor2, legendaryFloat1, legendaryFloat2, legendaryFloat3;

        public void Find(MaterialProperty[] props)
        {
            effectType = FindProperty("_EffectType", props);
            effectTex1 = FindProperty("_EffectTex1", props);
            rareColor1 = FindProperty("_RareColor1", props);
            rareFloat1 = FindProperty("_RareFloat1", props);
            rareFloat2 = FindProperty("_RareFloat2", props);
            epicColor1 = FindProperty("_EpicColor1", props);
            epicColor2 = FindProperty("_EpicColor2", props);
            epicFloat1 = FindProperty("_EpicFloat1", props);
            epicFloat2 = FindProperty("_EpicFloat2", props);
            epicFloat3 = FindProperty("_EpicFloat3", props);
            legendaryColor1 = FindProperty("_LegendaryColor1", props);
            legendaryColor2 = FindProperty("_LegendaryColor2", props);
            legendaryFloat1 = FindProperty("_LegendaryFloat1", props);
            legendaryFloat2 = FindProperty("_LegendaryFloat2", props);
            legendaryFloat3 = FindProperty("_LegendaryFloat3", props);
        }
    }
    #endregion

    private BaseProps baseProps;
    private LightingProps lightingProps;
    private ToonShadingProps toonShadingProps;
    private MetallicProps metallicProps;
    private FoliageProps foliageProps;
    private BlingProps blingProps;
    private CosmicProps cosmicProps;
    private OutlineProps outlineProps;
    private DissolveProps dissolveProps;
    private TieredEffectProps tieredEffectProps;

    protected override void FindProperties()
    {
        baseProps.Find(properties);
        lightingProps.Find(properties);
        toonShadingProps.Find(properties);
        metallicProps.Find(properties);
        foliageProps.Find(properties);
        blingProps.Find(properties);
        cosmicProps.Find(properties);
        outlineProps.Find(properties);
        dissolveProps.Find(properties);
        tieredEffectProps.Find(properties);
    }

    protected override void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        if (materialEditor.targets.Length > 1)
        {
            EditorGUILayout.HelpBox("Multi-material editing is enabled. Some values may not be consistent across all materials.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("This is the HQ version of the shader. Select a Surface Type to see its settings.", MessageType.Info);
        }

        DrawSurfaceTypeSelector();
        EditorGUILayout.EndVertical();
    }

    private void DrawSurfaceTypeSelector()
    {
        EditorGUI.showMixedValue = baseProps.surfaceType.hasMixedValue;
        var currentType = (ToonOpaqueUIDrawer.SurfaceType)baseProps.surfaceType.floatValue;

        EditorGUI.BeginChangeCheck();
        var newType = (ToonOpaqueUIDrawer.SurfaceType)EditorGUILayout.EnumPopup("Surface Type", currentType);
        if (EditorGUI.EndChangeCheck())
        {
            baseProps.surfaceType.floatValue = (float)newType;
        }
        EditorGUI.showMixedValue = false;
    }

    protected override void DrawMainProperties()
    {
        DrawRenderStates();
        DrawBaseSettings();
        DrawLightingSettings();
        DrawSurfaceSpecificSettings();

        DrawFoldout("Tiered Overlay Effects", ref showTieredEffectSettings, () => TieredEffectsDrawer.DrawTieredEffectsSettings(materialEditor, tieredEffectProps));
        DrawFoldout("Dissolve & Reveal Effects", ref showDissolveSettings, () => ToonOpaqueUIDrawer.DrawDissolveSettings(materialEditor, dissolveProps));
        DrawFoldout("Outline", ref showOutlineSettings, () => DrawOutlineSettings());
    }

    private void DrawBaseSettings()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(baseProps.baseMap.displayName), baseProps.baseMap, baseProps.baseColor);
            materialEditor.TexturePropertySingleLine(new GUIContent(baseProps.bumpMap.displayName), baseProps.bumpMap, baseProps.bumpScale);

            var renderMode = (RenderMode)baseProps.renderMode.floatValue;
            if (renderMode == RenderMode.Cutout)
            {
                materialEditor.ShaderProperty(baseProps.cutoff, baseProps.cutoff.displayName);
            }

            materialEditor.ShaderProperty(baseProps.emissionMode, baseProps.emissionMode.displayName);
            if (baseProps.emissionMode.floatValue > 0)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(baseProps.emissionColor, "Emission Color");
                materialEditor.TexturePropertySingleLine(new GUIContent(baseProps.emissionMap.displayName), baseProps.emissionMap);
                EditorGUI.indentLevel--;
            }
        });
    }

    private void DrawRenderStates()
    {
        DrawFoldout("Render States", ref showRenderStates, () =>
        {
            materialEditor.ShaderProperty(baseProps.renderMode, baseProps.renderMode.displayName);

            var renderMode = (RenderMode)baseProps.renderMode.floatValue;
            if (renderMode == RenderMode.Transparent)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(baseProps.srcBlend, baseProps.srcBlend.displayName);
                materialEditor.ShaderProperty(baseProps.dstBlend, baseProps.dstBlend.displayName);
                EditorGUI.indentLevel--;
            }

            materialEditor.ShaderProperty(baseProps.zWrite, baseProps.zWrite.displayName);
            materialEditor.ShaderProperty(baseProps.zTest, baseProps.zTest.displayName);
            materialEditor.ShaderProperty(baseProps.cullMode, baseProps.cullMode.displayName);
        });
    }

    private void DrawLightingSettings()
    {
        DrawFoldout("Lighting", ref showLightingSettings, () =>
        {
            materialEditor.ShaderProperty(lightingProps.fakeLightMode, lightingProps.fakeLightMode.displayName);
            if (lightingProps.fakeLightMode.floatValue > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Fake Light acts as a fallback when no main Directional Light is present.", MessageType.Info);
                materialEditor.ShaderProperty(lightingProps.fakeLightColor, "Color");
                materialEditor.ShaderProperty(lightingProps.fakeLightDirection, "Direction");
                EditorGUI.indentLevel--;
            }
        });
    }

    private void DrawSurfaceSpecificSettings()
    {
        if (baseProps.surfaceType.hasMixedValue)
        {
            EditorGUILayout.HelpBox("Surface types are mixed. Cannot display specific settings.", MessageType.Warning);
            return;
        }

        var surface = (ToonOpaqueUIDrawer.SurfaceType)baseProps.surfaceType.floatValue;
        switch (surface)
        {
            case ToonOpaqueUIDrawer.SurfaceType.Opaque:
                DrawFoldout("Toon Shading HQ", ref showToonSettings, () =>
                {
                    ToonOpaqueUIDrawer.DrawToonSettings(materialEditor, toonShadingProps);
                    EditorGUILayout.Space();
                    materialEditor.ColorProperty(lightingProps.ambientColor, "Ambient Color");
                    EditorGUILayout.HelpBox("Use Alpha to blend between Scene Ambient (A=0) and this custom color (A=1).", MessageType.Info);
                });
                break;
            case ToonOpaqueUIDrawer.SurfaceType.Metallic:
                DrawFoldout("Stylized Metal", ref showMetallicSettings, () => ToonOpaqueUIDrawer.DrawMetallicSettings(materialEditor, metallicProps));
                break;
            case ToonOpaqueUIDrawer.SurfaceType.Foliage:
                DrawFoldout("Foliage", ref showFoliageSettings, () => ToonOpaqueUIDrawer.DrawFoliageSettings(materialEditor, foliageProps));
                break;
            case ToonOpaqueUIDrawer.SurfaceType.Bling:
                DrawFoldout("Bling Effect (Procedural)", ref showBlingSettings, () => ToonOpaqueUIDrawer.DrawBlingSettings(materialEditor, blingProps));
                break;
            case ToonOpaqueUIDrawer.SurfaceType.Cosmic:
                DrawFoldout("Cosmic Galaxy Effect", ref showCosmicSettings, () => ToonOpaqueUIDrawer.DrawCosmicSettings(materialEditor, cosmicProps, metallicProps));
                break;
        }
    }

    private void DrawOutlineSettings()
    {
        materialEditor.ShaderProperty(outlineProps.outlineMode, "Mode");
        if (outlineProps.outlineMode.hasMixedValue) return;

        var currentMode = (OutlineMode)outlineProps.outlineMode.floatValue;
        if (currentMode == OutlineMode.Fresnel)
        {
            EditorGUILayout.LabelField("Fresnel Outline", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(outlineProps.fresnelColor, "Color");
            materialEditor.ShaderProperty(outlineProps.fresnelWidth, "Width");
            materialEditor.ShaderProperty(outlineProps.fresnelPower, "Power");
            materialEditor.ShaderProperty(outlineProps.fresnelSharpness, "Sharpness");
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(outlineProps.glintToggle, outlineProps.glintToggle.displayName);
            if (outlineProps.glintToggle.floatValue > 0)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(outlineProps.glintColor, "Glint Color");
                materialEditor.ShaderProperty(outlineProps.glintScale, "Glint Scale");
                materialEditor.ShaderProperty(outlineProps.glintSpeed, "Glint Speed");
                materialEditor.ShaderProperty(outlineProps.glintThreshold, "Glint Threshold");
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }
        else if (currentMode == OutlineMode.Hull)
        {
            EditorGUILayout.LabelField("Hull Outline", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(outlineProps.hullColor, "Color");
            materialEditor.ShaderProperty(outlineProps.hullWidth, "Width");
            materialEditor.ShaderProperty(outlineProps.hullScaleWithDistance, "Screen-Space Scaling");
            materialEditor.ShaderProperty(outlineProps.hullFadeStart, "Distance Fade Start");
            materialEditor.ShaderProperty(outlineProps.hullFadeEnd, "Distance Fade End");
            EditorGUI.indentLevel--;
        }
    }

    protected override void ApplyKeywords()
    {
        foreach (var mat in materials)
        {
            ApplyKeywordsToMaterial(mat);
        }
    }

    private void ApplyKeywordsToMaterial(Material mat)
    {
        var surface = (ToonOpaqueUIDrawer.SurfaceType)mat.GetFloat("_SurfaceType");
        SetKeywordForMaterial(mat, "_SURFACETYPE_OPAQUE", surface == ToonOpaqueUIDrawer.SurfaceType.Opaque);
        SetKeywordForMaterial(mat, "_SURFACETYPE_METALLIC", surface == ToonOpaqueUIDrawer.SurfaceType.Metallic);
        SetKeywordForMaterial(mat, "_SURFACETYPE_FOLIAGE", surface == ToonOpaqueUIDrawer.SurfaceType.Foliage);
        SetKeywordForMaterial(mat, "_SURFACETYPE_BLING", surface == ToonOpaqueUIDrawer.SurfaceType.Bling);
        SetKeywordForMaterial(mat, "_SURFACETYPE_COSMIC", surface == ToonOpaqueUIDrawer.SurfaceType.Cosmic);

        var outline = (OutlineMode)mat.GetFloat("_OutlineMode");
        SetKeywordForMaterial(mat, "_OUTLINEMODE_FRESNEL", outline == OutlineMode.Fresnel);
        SetKeywordForMaterial(mat, "_OUTLINE_SCALE_WITH_DISTANCE", outline == OutlineMode.Hull && mat.GetFloat("_OutlineScaleWithDistance") > 0.5f);
        SetKeywordForMaterial(mat, "_OUTLINEGLINT_ON", outline == OutlineMode.Fresnel && mat.GetFloat("_GlintToggle") > 0.5f);

        mat.SetShaderPassEnabled("Outline", outline == OutlineMode.Hull);

        SetKeywordForMaterial(mat, "_BLING_WORLDSPACE_ON", surface == ToonOpaqueUIDrawer.SurfaceType.Bling && mat.GetFloat("_BlingWorldSpace") > 0.5f);

        var renderMode = (RenderMode)mat.GetFloat("_RenderMode");
        SetKeywordForMaterial(mat, "_ALPHACLIP_ON", renderMode == RenderMode.Cutout);

        ApplyDissolveKeywordsToMaterial(mat);
        ApplyTieredEffectKeywordsToMaterial(mat);
        UpdateRenderStates(mat);
    }

    private void ApplyDissolveKeywordsToMaterial(Material mat)
    {
        bool dissolveOn = mat.GetFloat("_EnableDissolve") > 0.5f;
        SetKeywordForMaterial(mat, "_DISSOLVE_ON", dissolveOn);

        if (dissolveOn)
        {
            SetKeywordForMaterial(mat, "_HOLOGRAM_REVEAL_ON", mat.GetFloat("_EnableHologramReveal") > 0.5f);
            var currentDissolve = (ToonOpaqueUIDrawer.DissolveType)mat.GetFloat("_DissolveType");
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_NOISE", currentDissolve == ToonOpaqueUIDrawer.DissolveType.Noise);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_LINEAR", currentDissolve == ToonOpaqueUIDrawer.DissolveType.Linear);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_RADIAL", currentDissolve == ToonOpaqueUIDrawer.DissolveType.Radial);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_PATTERN", currentDissolve == ToonOpaqueUIDrawer.DissolveType.Pattern);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_ALPHA_BLEND", currentDissolve == ToonOpaqueUIDrawer.DissolveType.AlphaBlend);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_SHATTER", currentDissolve == ToonOpaqueUIDrawer.DissolveType.Shatter);
            SetKeywordForMaterial(mat, "_DISSOLVE_LOCALSPACE_ON", mat.GetFloat("_UseLocalSpace") > 0.5f);
            bool displacementOn = mat.GetFloat("_EnableVertexDisplacement") > 0.5f && currentDissolve != ToonOpaqueUIDrawer.DissolveType.Shatter;
            SetKeywordForMaterial(mat, "_VERTEX_DISPLACEMENT_ON", displacementOn);
            SetKeywordForMaterial(mat, "_DISPLACEMENT_SATURATE_ON", displacementOn && mat.GetFloat("_UseSaturateDisplacement") > 0.5f);
            SetKeywordForMaterial(mat, "_SHATTER_EFFECT_ON", mat.GetFloat("_EnableShatterEffect") > 0.5f);
        }
        else
        {
            SetKeywordForMaterial(mat, "_HOLOGRAM_REVEAL_ON", false);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_NOISE", false);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_LINEAR", false);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_RADIAL", false);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_PATTERN", false);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_ALPHA_BLEND", false);
            SetKeywordForMaterial(mat, "_DISSOLVETYPE_SHATTER", false);
            SetKeywordForMaterial(mat, "_DISSOLVE_LOCALSPACE_ON", false);
            SetKeywordForMaterial(mat, "_VERTEX_DISPLACEMENT_ON", false);
            SetKeywordForMaterial(mat, "_DISPLACEMENT_SATURATE_ON", false);
            SetKeywordForMaterial(mat, "_SHATTER_EFFECT_ON", false);
        }
    }

    private void ApplyTieredEffectKeywordsToMaterial(Material mat)
    {
        var effectType = (TieredEffectsDrawer.EffectType)mat.GetFloat("_EffectType");
        SetKeywordForMaterial(mat, "_EFFECT_RARE_PULSING_GLOW", effectType == TieredEffectsDrawer.EffectType.Rare_PulsingGlow);
        SetKeywordForMaterial(mat, "_EFFECT_RARE_SPARKLES", effectType == TieredEffectsDrawer.EffectType.Rare_Sparkles);
        SetKeywordForMaterial(mat, "_EFFECT_EPIC_FIRE_AURA", effectType == TieredEffectsDrawer.EffectType.Epic_FireAura);
        SetKeywordForMaterial(mat, "_EFFECT_EPIC_ELECTRIC_FIELD", effectType == TieredEffectsDrawer.EffectType.Epic_ElectricField);
        SetKeywordForMaterial(mat, "_EFFECT_LEGENDARY_COSMIC_RIFT", effectType == TieredEffectsDrawer.EffectType.Legendary_CosmicRift);
        SetKeywordForMaterial(mat, "_EFFECT_LEGENDARY_HOLY_AURA", effectType == TieredEffectsDrawer.EffectType.Legendary_HolyAura);
    }

    private void UpdateRenderStates(Material mat)
    {
        var renderMode = (RenderMode)mat.GetFloat("_RenderMode");

        switch (renderMode)
        {
            case RenderMode.Opaque:
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                break;
            case RenderMode.Cutout:
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                break;
            case RenderMode.Transparent:
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
        }
    }

    private void SetKeywordForMaterial(Material mat, string keyword, bool state)
    {
        if (state) mat.EnableKeyword(keyword);
        else mat.DisableKeyword(keyword);
    }
}