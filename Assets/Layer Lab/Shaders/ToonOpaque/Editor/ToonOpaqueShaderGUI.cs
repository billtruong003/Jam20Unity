using UnityEngine;
using UnityEditor;

public class ToonOpaqueShaderGUI : ToonOpaqueShaderBase
{
    private static bool showFresnelOutlineSettings = true;

    private MaterialProperty fresnelOutlineToggleProp, fresnelOutlineColorProp, fresnelOutlineWidthProp, fresnelOutlinePowerProp, fresnelOutlineSharpnessProp;
    private MaterialProperty glintToggleProp, glintColorProp, glintScaleProp, glintSpeedProp, glintThresholdProp;

    protected override void FindProperties()
    {
        base.FindProperties();

        fresnelOutlineToggleProp = FindProperty("_FresnelOutlineToggle", properties);
        fresnelOutlineColorProp = FindProperty("_FresnelOutlineColor", properties);
        fresnelOutlineWidthProp = FindProperty("_FresnelOutlineWidth", properties);
        fresnelOutlinePowerProp = FindProperty("_FresnelOutlinePower", properties);
        fresnelOutlineSharpnessProp = FindProperty("_FresnelOutlineSharpness", properties);

        glintToggleProp = FindProperty("_GlintToggle", properties);
        glintColorProp = FindProperty("_GlintColor", properties);
        glintScaleProp = FindProperty("_GlintScale", properties);
        glintSpeedProp = FindProperty("_GlintSpeed", properties);
        glintThresholdProp = FindProperty("_GlintThreshold", properties);
    }

    protected override void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a Surface Type to unlock its specific settings. Use the buttons below to manage outlines.", MessageType.Info);

        DrawSurfaceTypeSelector();

        EditorGUILayout.LabelField("Outline Mode", "None / Fresnel");
        if (GUILayout.Button("Add Inverted Hull Outline (Switch Shader)"))
        {
            if (EditorUtility.DisplayDialog("Switch Shader?", "This will switch to the 'Opaque (Hull Outline)' shader. Are you sure?", "Yes", "No"))
            {
                SwitchShader("Bill's Toon/Opaque (Hull Outline)");
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected override void DrawMainProperties()
    {
        base.DrawMainProperties();
        DrawFoldout("Fresnel Outline", ref showFresnelOutlineSettings, () =>
        {
            DrawPropertyGroup(fresnelOutlineToggleProp, fresnelOutlineToggleProp.displayName, () =>
            {
                materialEditor.ShaderProperty(fresnelOutlineColorProp, "Color");
                materialEditor.ShaderProperty(fresnelOutlineWidthProp, "Width");
                materialEditor.ShaderProperty(fresnelOutlinePowerProp, "Power");
                materialEditor.ShaderProperty(fresnelOutlineSharpnessProp, "Sharpness");
                EditorGUILayout.Space();

                bool fresnelGroupEnabled = fresnelOutlineToggleProp.floatValue > 0.5f || fresnelOutlineToggleProp.hasMixedValue;
                EditorGUI.BeginDisabledGroup(!fresnelGroupEnabled);
                DrawPropertyGroup(glintToggleProp, glintToggleProp.displayName, () =>
                {
                    materialEditor.ShaderProperty(glintColorProp, "Glint Color");
                    materialEditor.ShaderProperty(glintScaleProp, "Glint Scale");
                    materialEditor.ShaderProperty(glintSpeedProp, "Glint Speed");
                    materialEditor.ShaderProperty(glintThresholdProp, "Glint Threshold");
                });
                EditorGUI.EndDisabledGroup();
            });
        });
    }

    protected override void ApplyKeywords()
    {
        base.ApplyKeywords();

        bool fresnelOn = fresnelOutlineToggleProp.floatValue > 0.5f;
        SetKeyword("_OUTLINEMODE_FRESNEL", fresnelOn);

        bool glintOn = fresnelOn && glintToggleProp.floatValue > 0.5f;
        SetKeyword("_OUTLINEGLINT_ON", glintOn);
    }
}