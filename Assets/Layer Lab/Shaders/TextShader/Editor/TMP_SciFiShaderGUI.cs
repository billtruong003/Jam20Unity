using UnityEngine;
using UnityEditor;
using System;

public class SciFiShaderUltimateGUI_V3_Configurable : ShaderGUI
{
    private MaterialEditor materialEditor;
    private bool firstTimeApply = true;

    // Property Cache
    private MaterialProperty faceColor, faceDilate, outlineColor, outlineWidth, outlineSoftness, glowColor;
    private MaterialProperty effectMode, effectTintColor;
    private MaterialProperty noiseTex, glitchToggle, glitchStrength, glitchSpeed;
    private MaterialProperty scanLinesToggle, scanLinesDensity, scanLinesSpeed, scanLinesIntensity;
    private MaterialProperty chromaticToggle, chromaticAberrationAmount;
    private MaterialProperty holoGridToggle, holoGridColor, holoGridTiling, holoGridSpeed;
    private MaterialProperty fireGradient, fireSpeed, fireTurbulence, fireGlowIntensity;
    private MaterialProperty waterSpeed, waterDistortion, causticsTex, causticsTiling, causticsIntensity;
    private MaterialProperty iceCrystalTex, iceRefraction, frostAmount;
    private MaterialProperty zTest, zWrite, cullMode;

    // Foldout States
    private static bool showFaceProperties = true;
    private static bool showEffectProperties = true;
    private static bool showAdvancedOptions = false;

    // GUIContent
    private static readonly GUIContent gc_glitchStrength = new GUIContent("Overall Strength", "Controls intensity of all glitch effects.");
    private static readonly GUIContent gc_glitchSpeed = new GUIContent("Speed", "Controls animation speed of the glitch effect.");
    private static readonly GUIContent gc_chromaticAmount = new GUIContent("Amount", "The strength of the constant color fringe.");
    private static readonly GUIContent gc_noiseTex = new GUIContent("Noise Texture", "A seamless, tileable noise texture is required for all effects.");

    private void FindProperties(MaterialProperty[] props)
    {
        faceColor = FindProperty("_FaceColor", props);
        faceDilate = FindProperty("_FaceDilate", props);
        outlineColor = FindProperty("_OutlineColor", props);
        outlineWidth = FindProperty("_OutlineWidth", props);
        outlineSoftness = FindProperty("_OutlineSoftness", props);
        glowColor = FindProperty("_GlowColor", props);
        effectMode = FindProperty("_EffectMode", props);
        effectTintColor = FindProperty("_EffectTintColor", props);
        noiseTex = FindProperty("_NoiseTex", props);
        glitchToggle = FindProperty("_GlitchToggle", props);
        glitchStrength = FindProperty("_GlitchStrength", props);
        glitchSpeed = FindProperty("_GlitchSpeed", props);
        scanLinesToggle = FindProperty("_ScanLinesToggle", props);
        scanLinesDensity = FindProperty("_ScanLinesDensity", props);
        scanLinesSpeed = FindProperty("_ScanLinesSpeed", props);
        scanLinesIntensity = FindProperty("_ScanLinesIntensity", props);
        chromaticToggle = FindProperty("_ChromaticToggle", props);
        chromaticAberrationAmount = FindProperty("_ChromaticAberrationAmount", props);
        holoGridToggle = FindProperty("_HoloGridToggle", props);
        holoGridColor = FindProperty("_HoloGridColor", props);
        holoGridTiling = FindProperty("_HoloGridTiling", props);
        holoGridSpeed = FindProperty("_HoloGridSpeed", props);
        fireGradient = FindProperty("_FireGradient", props);
        fireSpeed = FindProperty("_FireSpeed", props);
        fireTurbulence = FindProperty("_FireTurbulence", props);
        fireGlowIntensity = FindProperty("_FireGlowIntensity", props);
        waterSpeed = FindProperty("_WaterSpeed", props);
        waterDistortion = FindProperty("_WaterDistortion", props);
        causticsTex = FindProperty("_CausticsTex", props);
        causticsTiling = FindProperty("_CausticsTiling", props);
        causticsIntensity = FindProperty("_CausticsIntensity", props);
        iceCrystalTex = FindProperty("_IceCrystalTex", props);
        iceRefraction = FindProperty("_IceRefraction", props);
        frostAmount = FindProperty("_FrostAmount", props);

        zTest = FindProperty("_ZTest", props);
        zWrite = FindProperty("_ZWrite", props);
        cullMode = FindProperty("_CullMode", props);
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        if (firstTimeApply)
        {
            FindProperties(props);
            firstTimeApply = false;
        }

        DrawFaceProperties();
        DrawMasterEffectSelector();
        DrawEffectProperties();
        DrawAdvancedRenderOptions();
    }

    private void DrawHeader(string title)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private void DrawToggledSection(MaterialProperty toggle, Action drawFunction)
    {
        materialEditor.ShaderProperty(toggle, toggle.displayName);
        if (toggle.floatValue > 0)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.indentLevel++;
            drawFunction();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(6);
    }

    private void DrawFaceProperties()
    {
        showFaceProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFaceProperties, "Face & Outline Properties");
        if (showFaceProperties)
        {
            EditorGUILayout.BeginVertical("box");
            DrawHeader("Face");
            materialEditor.ShaderProperty(faceColor, "Color");
            materialEditor.ShaderProperty(faceDilate, faceDilate.displayName);
            DrawHeader("Outline");
            materialEditor.ShaderProperty(outlineColor, "Color");
            materialEditor.ShaderProperty(outlineWidth, outlineWidth.displayName);
            materialEditor.ShaderProperty(outlineSoftness, outlineSoftness.displayName);
            DrawHeader("Glow");
            materialEditor.ShaderProperty(glowColor, "Global Glow");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawMasterEffectSelector()
    {
        DrawHeader("Master Effect Selection");
        materialEditor.ShaderProperty(effectMode, "Effect Mode");
    }

    private void DrawEffectProperties()
    {
        int currentMode = (int)effectMode.floatValue;
        if (currentMode == 0) return;

        showEffectProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showEffectProperties, "Effect Properties");
        if (showEffectProperties)
        {
            EditorGUILayout.BeginVertical("box");
            switch (currentMode)
            {
                case 1: DrawSciFiControls(); break;
                case 2: DrawFireControls(); break;
                case 3: DrawWaterControls(); break;
                case 4: DrawIceControls(); break;
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawAdvancedRenderOptions()
    {
        showAdvancedOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvancedOptions, "Advanced Rendering Options");
        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical("box");
            materialEditor.RenderQueueField();
            materialEditor.ShaderProperty(zTest, zTest.displayName);
            materialEditor.ShaderProperty(zWrite, zWrite.displayName);
            materialEditor.ShaderProperty(cullMode, cullMode.displayName);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawSciFiControls()
    {
        DrawHeader("General Sci-Fi");
        materialEditor.ShaderProperty(noiseTex, gc_noiseTex);
        EditorGUILayout.Space(6);
        DrawToggledSection(glitchToggle, () =>
        {
            materialEditor.ShaderProperty(glitchStrength, gc_glitchStrength);
            materialEditor.ShaderProperty(glitchSpeed, gc_glitchSpeed);
        });
        DrawToggledSection(chromaticToggle, () =>
        {
            materialEditor.ShaderProperty(chromaticAberrationAmount, gc_chromaticAmount);
        });
        DrawToggledSection(scanLinesToggle, () =>
        {
            materialEditor.ShaderProperty(scanLinesDensity, "Density");
            materialEditor.ShaderProperty(scanLinesSpeed, "Speed");
            materialEditor.ShaderProperty(scanLinesIntensity, "Intensity");
        });
        DrawToggledSection(holoGridToggle, () =>
        {
            materialEditor.ShaderProperty(holoGridColor, "Color");
            materialEditor.ShaderProperty(holoGridTiling, "Tiling");
            materialEditor.ShaderProperty(holoGridSpeed, "Speed");
        });
    }

    private void DrawFireControls()
    {
        materialEditor.ShaderProperty(effectTintColor, "Tint Color");
        DrawHeader("Textures");
        materialEditor.ShaderProperty(noiseTex, "Turbulence Noise");
        materialEditor.ShaderProperty(fireGradient, "Color Gradient");
        DrawHeader("Parameters");
        materialEditor.ShaderProperty(fireSpeed, "Speed");
        materialEditor.ShaderProperty(fireTurbulence, "Turbulence");
        materialEditor.ShaderProperty(fireGlowIntensity, "Glow Intensity");
    }

    private void DrawWaterControls()
    {
        materialEditor.ShaderProperty(effectTintColor, "Tint Color");
        DrawHeader("Distortion");
        materialEditor.ShaderProperty(noiseTex, "Wave Noise");
        materialEditor.ShaderProperty(waterSpeed, "Flow Speed");
        materialEditor.ShaderProperty(waterDistortion, "Distortion Amount");
        DrawHeader("Caustics");
        materialEditor.ShaderProperty(causticsTex, "Caustics Texture");
        materialEditor.ShaderProperty(causticsTiling, "Tiling");
        materialEditor.ShaderProperty(causticsIntensity, "Intensity");
    }

    private void DrawIceControls()
    {
        materialEditor.ShaderProperty(effectTintColor, "Tint Color");
        DrawHeader("Textures");
        materialEditor.ShaderProperty(noiseTex, "Frost Noise");
        materialEditor.ShaderProperty(iceCrystalTex, "Crystal Texture");
        DrawHeader("Parameters");
        materialEditor.ShaderProperty(iceRefraction, "Inner Refraction");
        materialEditor.ShaderProperty(frostAmount, "Frost Amount");
    }
}