using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;
using System.Linq;

public abstract class ToonOpaqueShaderBase : ShaderGUI
{
    protected MaterialEditor materialEditor;
    protected MaterialProperty[] properties;
    protected Material firstMaterial;
    protected Material[] materials;

    private static bool showRenderStates = true;
    private static bool showBaseSettings = true;
    private static bool showDitherFadeSettings = true;
    private static bool showLightingSettings = true;
    private static bool showIndirectLightingSettings = true;
    private static bool showAdvancedSettings = false;
    private static GUIStyle headerStyle;

    protected MaterialProperty surfaceTypeProp, renderModeProp, srcBlendProp, dstBlendProp, zWriteProp;
    protected MaterialProperty baseMapProp, baseColorProp, bumpMapProp, bumpScaleProp;
    protected MaterialProperty cutoffProp, emissionModeProp, emissionColorProp, emissionMapProp;
    protected MaterialProperty ditherFadeToggleProp, ditherPatternTexProp, ditherScaleProp, ditherFadeStartProp, ditherFadeEndProp, ditherEdgeColorProp, ditherEdgeWidthProp;
    protected MaterialProperty cullModeProp, forceFakeLightProp, fakeLightModeProp, fakeLightColorProp, fakeLightDirectionProp, ambientColorProp, maxBrightnessProp;
    protected MaterialProperty toonStyleProp, shadowThresholdProp, midtoneThresholdProp, toonRampSmoothnessProp, shadowTintProp, midtoneColorProp;
    protected MaterialProperty addLightShadowTintProp, addLightMidtoneColorProp, addLightShadowThresholdProp, addLightMidtoneThresholdProp, addLightRampSmoothnessProp;
    protected MaterialProperty indirectSpecularToggleProp, indirectSpecularIntensityProp;
    protected MaterialProperty rampProp, brightnessProp, offsetProp, specuColorProp, highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp;
    protected MaterialProperty windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp;
    protected MaterialProperty noiseTexProp, blingWorldSpaceProp, blingColorProp, blingIntensityProp, blingScaleProp, blingSpeedProp, blingFresnelPowerProp, blingThresholdProp;
    protected MaterialProperty morphToggleProp, baseMapBProp, morphProp;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        properties = props;
        materials = Array.ConvertAll(editor.targets, item => (Material)item);
        firstMaterial = materials[0];

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
        }

        FindProperties();

        EditorGUI.BeginChangeCheck();
        DrawGUI();
        if (EditorGUI.EndChangeCheck())
        {
            ApplyKeywords();
        }
    }

    protected virtual void FindProperties()
    {
        surfaceTypeProp = FindProperty("_SurfaceType", properties);
        renderModeProp = FindProperty("_RenderMode", properties);
        srcBlendProp = FindProperty("_SrcBlend", properties);
        dstBlendProp = FindProperty("_DstBlend", properties);
        zWriteProp = FindProperty("_ZWrite", properties);

        baseMapProp = FindProperty("_BaseMap", properties);
        baseColorProp = FindProperty("_BaseColor", properties);
        bumpMapProp = FindProperty("_BumpMap", properties);
        bumpScaleProp = FindProperty("_BumpScale", properties);

        cutoffProp = FindProperty("_Cutoff", properties);
        emissionModeProp = FindProperty("_EmissionMode", properties);
        emissionColorProp = FindProperty("_EmissionColor", properties);
        emissionMapProp = FindProperty("_EmissionMap", properties);

        ditherFadeToggleProp = FindProperty("_DitherFadeToggle", properties);
        ditherPatternTexProp = FindProperty("_DitherPatternTex", properties);
        ditherScaleProp = FindProperty("_DitherScale", properties);
        ditherFadeStartProp = FindProperty("_DitherFadeStart", properties);
        ditherFadeEndProp = FindProperty("_DitherFadeEnd", properties);
        ditherEdgeColorProp = FindProperty("_DitherEdgeColor", properties);
        ditherEdgeWidthProp = FindProperty("_DitherEdgeWidth", properties);

        cullModeProp = FindProperty("_CullMode", properties, false);
        forceFakeLightProp = FindProperty("_ForceFakeLight", properties);
        fakeLightModeProp = FindProperty("_FakeLightMode", properties);
        fakeLightColorProp = FindProperty("_FakeLightColor", properties);
        fakeLightDirectionProp = FindProperty("_FakeLightDirection", properties);
        ambientColorProp = FindProperty("_AmbientColor", properties);
        maxBrightnessProp = FindProperty("_MaxBrightness", properties);

        toonStyleProp = FindProperty("_ToonStyle", properties);
        shadowThresholdProp = FindProperty("_ShadowThreshold", properties);
        midtoneThresholdProp = FindProperty("_MidtoneThreshold", properties);
        toonRampSmoothnessProp = FindProperty("_ToonRampSmoothness", properties);
        shadowTintProp = FindProperty("_ShadowTint", properties);
        midtoneColorProp = FindProperty("_MidtoneColor", properties);

        addLightShadowTintProp = FindProperty("_AddLightShadowTint", properties);
        addLightMidtoneColorProp = FindProperty("_AddLightMidtoneColor", properties);
        addLightShadowThresholdProp = FindProperty("_AddLightShadowThreshold", properties);
        addLightMidtoneThresholdProp = FindProperty("_AddLightMidtoneThreshold", properties);
        addLightRampSmoothnessProp = FindProperty("_AddLightRampSmoothness", properties);

        indirectSpecularToggleProp = FindProperty("_IndirectSpecular", properties);
        indirectSpecularIntensityProp = FindProperty("_IndirectSpecularIntensity", properties);

        rampProp = FindProperty("_Ramp", properties);
        brightnessProp = FindProperty("_Brightness", properties);
        offsetProp = FindProperty("_Offset", properties);
        specuColorProp = FindProperty("_SpecuColor", properties);
        highlightOffsetProp = FindProperty("_HighlightOffset", properties);
        hiColorProp = FindProperty("_HiColor", properties);
        rimColorProp = FindProperty("_RimColor", properties);
        rimPowerProp = FindProperty("_RimPower", properties);

        windFrequencyProp = FindProperty("_WindFrequency", properties);
        windAmplitudeProp = FindProperty("_WindAmplitude", properties);
        windDirectionProp = FindProperty("_WindDirection", properties);
        translucencyColorProp = FindProperty("_TranslucencyColor", properties);
        translucencyStrengthProp = FindProperty("_TranslucencyStrength", properties);

        noiseTexProp = FindProperty("_NoiseTex", properties);
        blingWorldSpaceProp = FindProperty("_BlingWorldSpace", properties);
        blingColorProp = FindProperty("_BlingColor", properties);
        blingIntensityProp = FindProperty("_BlingIntensity", properties);
        blingScaleProp = FindProperty("_BlingScale", properties);
        blingSpeedProp = FindProperty("_BlingSpeed", properties);
        blingFresnelPowerProp = FindProperty("_BlingFresnelPower", properties);
        blingThresholdProp = FindProperty("_BlingThreshold", properties);

        morphToggleProp = FindProperty("_MorphToggle", properties, false);
        baseMapBProp = FindProperty("_BaseMapB", properties, false);
        morphProp = FindProperty("_Morph", properties, false);
    }

    private void DrawGUI()
    {
        DrawHeader();
        DrawWorkflowSettings();
        EditorGUILayout.Space();
        DrawMainProperties();
        EditorGUILayout.Space();
        DrawAdvancedSettings();
    }

    protected virtual void DrawMainProperties()
    {
        DrawRenderStates();
        DrawBaseProperties();
        DrawDitherFade();
        DrawLighting();
        DrawIndirectLighting();
        DrawSurfaceTypeSpecificProperties();
    }

    protected virtual void ApplyKeywords()
    {
        foreach (var mat in materials)
        {
            ApplyRenderModeKeywords(mat);
        }

        var surface = (ToonOpaqueDrawerUtils.SurfaceType)surfaceTypeProp.floatValue;
        SetKeyword("_SURFACETYPE_OPAQUE", surface == ToonOpaqueDrawerUtils.SurfaceType.Opaque);
        SetKeyword("_SURFACETYPE_METALLIC", surface == ToonOpaqueDrawerUtils.SurfaceType.Metallic);
        SetKeyword("_SURFACETYPE_FOLIAGE", surface == ToonOpaqueDrawerUtils.SurfaceType.Foliage);
        SetKeyword("_SURFACETYPE_BLING", surface == ToonOpaqueDrawerUtils.SurfaceType.Bling);

        SetKeyword("_NORMALMAP_ON", bumpMapProp.textureValue != null);
        SetKeyword("_EMISSION_ON", emissionModeProp.floatValue > 0.5f);

        bool isBlingActive = surface == ToonOpaqueDrawerUtils.SurfaceType.Bling;
        SetKeyword("_BLING_WORLDSPACE_ON", isBlingActive && blingWorldSpaceProp.floatValue > 0.5f);

        SetKeyword("_DITHERFADE_ON", ditherFadeToggleProp.floatValue > 0.5f);
        SetKeyword("_INDIRECTSPECULAR_ON", indirectSpecularToggleProp.floatValue > 0.5f);

        bool isToonOpaque = surface == ToonOpaqueDrawerUtils.SurfaceType.Opaque;
        SetKeyword("_TOON_STYLE_HARD", isToonOpaque && toonStyleProp.floatValue > 0.5f);

        bool forceFakeLight = forceFakeLightProp.floatValue > 0.5f;
        SetKeyword("_FORCE_FAKELIGHT_ON", forceFakeLight);
        SetKeyword("_FAKELIGHT_ON", forceFakeLight || fakeLightModeProp.floatValue > 0.5f);

        if (morphToggleProp != null)
        {
            bool morphOn = morphToggleProp.floatValue > 0.5f && baseMapBProp != null && baseMapBProp.textureValue != null;
            SetKeyword("_MORPH_ON", morphOn);
        }
    }

    protected abstract void DrawWorkflowSettings();

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Bill's Toon Shader", headerStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    protected void DrawSurfaceTypeSelector()
    {
        EditorGUI.showMixedValue = surfaceTypeProp.hasMixedValue;
        var currentSurfaceType = (ToonOpaqueDrawerUtils.SurfaceType)surfaceTypeProp.floatValue;

        EditorGUI.BeginChangeCheck();
        var newSurfaceType = (ToonOpaqueDrawerUtils.SurfaceType)EditorGUILayout.EnumPopup("Surface Type", currentSurfaceType);
        if (EditorGUI.EndChangeCheck())
        {
            surfaceTypeProp.floatValue = (float)newSurfaceType;
        }
        EditorGUI.showMixedValue = false;
    }

    private void DrawRenderStates()
    {
        DrawFoldout("Render States", ref showRenderStates, () =>
        {
            EditorGUI.showMixedValue = renderModeProp.hasMixedValue;
            var mode = (RenderMode)renderModeProp.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (RenderMode)EditorGUILayout.EnumPopup("Render Mode", mode);
            if (EditorGUI.EndChangeCheck())
            {
                renderModeProp.floatValue = (float)mode;
            }
            EditorGUI.showMixedValue = false;

            if (!renderModeProp.hasMixedValue && mode == RenderMode.Transparent)
            {
                materialEditor.ShaderProperty(srcBlendProp, "Source Blend");
                materialEditor.ShaderProperty(dstBlendProp, "Destination Blend");
            }

            materialEditor.ShaderProperty(zWriteProp, "ZWrite");
            if (cullModeProp != null) materialEditor.ShaderProperty(cullModeProp, cullModeProp.displayName);
        });
    }

    private void DrawBaseProperties()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(baseMapProp.displayName), baseMapProp, baseColorProp);
            materialEditor.TexturePropertySingleLine(new GUIContent(bumpMapProp.displayName), bumpMapProp, bumpScaleProp);

            if (morphToggleProp != null)
            {
                DrawPropertyGroup(morphToggleProp, "Enable Morph", () =>
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent(baseMapBProp.displayName), baseMapBProp);
                    materialEditor.ShaderProperty(morphProp, morphProp.displayName);
                    if (baseMapBProp.textureValue == null && !baseMapBProp.hasMixedValue)
                    {
                        EditorGUILayout.HelpBox("Albedo B is not assigned. Morph will not be visible.", MessageType.Warning);
                    }
                });
            }

            if (!renderModeProp.hasMixedValue && (RenderMode)renderModeProp.floatValue == RenderMode.Cutout)
            {
                materialEditor.ShaderProperty(cutoffProp, cutoffProp.displayName);
            }

            DrawPropertyGroup(emissionModeProp, "Enable Emission", () =>
            {
                materialEditor.ShaderProperty(emissionColorProp, "Emission Color");
                materialEditor.TexturePropertySingleLine(new GUIContent(emissionMapProp.displayName), emissionMapProp);
            });
        });
    }

    private void DrawDitherFade()
    {
        DrawFoldout("Advanced Camera Dither Fade", ref showDitherFadeSettings, () =>
        {
            DrawPropertyGroup(ditherFadeToggleProp, "Enable Dither Fade", () =>
            {
                EditorGUILayout.HelpBox("Fades the object based on camera distance using a screen-space dither pattern.", MessageType.Info);
                materialEditor.TexturePropertySingleLine(new GUIContent(ditherPatternTexProp.displayName), ditherPatternTexProp);
                materialEditor.ShaderProperty(ditherScaleProp, "Pattern Scale");
                EditorGUILayout.Space();
                materialEditor.ShaderProperty(ditherFadeStartProp, "Fade Start Distance");
                materialEditor.ShaderProperty(ditherFadeEndProp, "Fade End Distance");
                EditorGUILayout.Space();
                materialEditor.ShaderProperty(ditherEdgeColorProp, "Edge Color");
                materialEditor.ShaderProperty(ditherEdgeWidthProp, "Edge Width");
            });
        });
    }

    private void DrawLighting()
    {
        DrawFoldout("Lighting", ref showLightingSettings, () =>
        {
            materialEditor.ShaderProperty(forceFakeLightProp, forceFakeLightProp.displayName);
            bool isForceFakeLightOn = forceFakeLightProp.floatValue > 0.5f;

            EditorGUI.BeginDisabledGroup(isForceFakeLightOn && !forceFakeLightProp.hasMixedValue);
            materialEditor.ShaderProperty(fakeLightModeProp, fakeLightModeProp.displayName);
            EditorGUI.EndDisabledGroup();

            if (isForceFakeLightOn && !forceFakeLightProp.hasMixedValue) fakeLightModeProp.floatValue = 1.0f;

            if (fakeLightModeProp.floatValue > 0.5f || fakeLightModeProp.hasMixedValue)
            {
                EditorGUI.indentLevel++;
                string helpText = isForceFakeLightOn ? "Fake Light is forced on, ignoring scene lighting." : "Fake Light acts as a fallback when no main Directional Light is present.";
                EditorGUILayout.HelpBox(helpText, MessageType.Info);
                materialEditor.ShaderProperty(fakeLightColorProp, "Color");
                materialEditor.ShaderProperty(fakeLightDirectionProp, "Direction");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            materialEditor.ColorProperty(ambientColorProp, "Ambient Color");
            EditorGUILayout.HelpBox("Use the Alpha channel to blend between Scene Ambient (A=0) and this custom color (A=1).", MessageType.Info);
            materialEditor.ShaderProperty(maxBrightnessProp, "Max Brightness");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Additional Lights", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(addLightShadowTintProp, "Shadow Tint");
            materialEditor.ShaderProperty(addLightMidtoneColorProp, "Mid-tone Color");
            materialEditor.ShaderProperty(addLightShadowThresholdProp, "Shadow Threshold");
            materialEditor.ShaderProperty(addLightMidtoneThresholdProp, "Mid-tone Threshold");
            materialEditor.ShaderProperty(addLightRampSmoothnessProp, "Ramp Smoothness");
            EditorGUI.indentLevel--;
        });
    }

    private void DrawIndirectLighting()
    {
        DrawFoldout("Indirect Lighting", ref showIndirectLightingSettings, () =>
        {
            EditorGUILayout.HelpBox("Diffuse GI is handled automatically. Shader will use Adaptive Probe Volumes if enabled in the URP Asset, otherwise it will fall back to Light Probes.", MessageType.Info);
            DrawPropertyGroup(indirectSpecularToggleProp, "Enable Environment Reflections", () =>
            {
                EditorGUILayout.HelpBox("Samples Reflection Probes or Skybox for specular reflections. Disabled for Metallic/Bling.", MessageType.Info);
                materialEditor.ShaderProperty(indirectSpecularIntensityProp, "Intensity");
            });
        });
    }

    private void DrawSurfaceTypeSpecificProperties()
    {
        if (surfaceTypeProp.hasMixedValue)
        {
            EditorGUILayout.HelpBox("Editing of surface-specific properties is disabled because materials with different Surface Types are selected.", MessageType.Warning);
            return;
        }

        var surface = (ToonOpaqueDrawerUtils.SurfaceType)surfaceTypeProp.floatValue;
        switch (surface)
        {
            case ToonOpaqueDrawerUtils.SurfaceType.Opaque:
                ToonOpaqueDrawerUtils.DrawToonSettings(materialEditor, toonStyleProp, shadowThresholdProp, midtoneThresholdProp, toonRampSmoothnessProp, shadowTintProp, midtoneColorProp);
                break;
            case ToonOpaqueDrawerUtils.SurfaceType.Metallic:
                ToonOpaqueDrawerUtils.DrawMetallicSettings(materialEditor, rampProp, brightnessProp, offsetProp, specuColorProp, highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp);
                break;
            case ToonOpaqueDrawerUtils.SurfaceType.Foliage:
                ToonOpaqueDrawerUtils.DrawFoliageSettings(materialEditor, windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp);
                break;
            case ToonOpaqueDrawerUtils.SurfaceType.Bling:
                ToonOpaqueDrawerUtils.DrawBlingSettings(materialEditor, noiseTexProp, blingWorldSpaceProp, blingColorProp, blingIntensityProp, blingScaleProp, blingSpeedProp, blingFresnelPowerProp, blingThresholdProp);
                break;
        }
    }

    private void DrawAdvancedSettings()
    {
        DrawFoldout("Advanced Settings", ref showAdvancedSettings, () =>
        {
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        });
    }

    private void ApplyRenderModeKeywords(Material material)
    {
        var mode = (RenderMode)renderModeProp.floatValue;
        switch (mode)
        {
            case RenderMode.Opaque:
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                SetKeywordOnMaterial(material, "_ALPHACLIP_ON", false);
                break;
            case RenderMode.Cutout:
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                SetKeywordOnMaterial(material, "_ALPHACLIP_ON", true);
                break;
            case RenderMode.Transparent:
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetInt("_SrcBlend", (int)srcBlendProp.floatValue);
                material.SetInt("_DstBlend", (int)dstBlendProp.floatValue);
                material.SetInt("_ZWrite", zWriteProp.floatValue > 0.5f ? 1 : 0);
                SetKeywordOnMaterial(material, "_ALPHACLIP_ON", false);
                break;
        }
    }

    protected void DrawFoldout(string title, ref bool state, Action contents)
    {
        state = EditorGUILayout.BeginFoldoutHeaderGroup(state, title);
        if (state)
        {
            EditorGUILayout.BeginVertical("box");
            contents.Invoke();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(2);
    }

    protected void DrawPropertyGroup(MaterialProperty toggleProp, string title, Action contents)
    {
        materialEditor.ShaderProperty(toggleProp, title);

        bool isGroupEnabled = toggleProp.floatValue > 0.5f || toggleProp.hasMixedValue;

        if (isGroupEnabled)
        {
            EditorGUI.indentLevel++;
            contents();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    protected void SetKeyword(string keyword, bool state)
    {
        foreach (var mat in materials)
        {
            SetKeywordOnMaterial(mat, keyword, state);
        }
    }

    private void SetKeywordOnMaterial(Material mat, string keyword, bool state)
    {
        if (state) mat.EnableKeyword(keyword);
        else mat.DisableKeyword(keyword);
    }

    protected void SwitchShader(string newShaderName)
    {
        var newShader = Shader.Find(newShaderName);
        if (newShader != null)
        {
            materialEditor.SetShader(newShader, true);
        }
        else
        {
            Debug.LogWarning($"Could not find shader '{newShaderName}'");
        }
    }

    protected enum RenderMode { Opaque, Cutout, Transparent }
}