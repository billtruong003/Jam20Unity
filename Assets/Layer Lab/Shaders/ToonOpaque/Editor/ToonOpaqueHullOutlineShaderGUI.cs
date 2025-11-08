using UnityEngine;
using UnityEditor;

public class ToonOpaqueHullOutlineShaderGUI : ToonOpaqueShaderBase
{
    private static bool showHullOutlineSettings = true;

    private MaterialProperty outlineColorProp, outlineWidthProp, outlineScaleWithDistanceProp, distanceFadeStartProp, distanceFadeEndProp;

    protected override void FindProperties()
    {
        base.FindProperties();

        outlineColorProp = FindProperty("_OutlineColor", properties);
        outlineWidthProp = FindProperty("_OutlineWidth", properties);
        outlineScaleWithDistanceProp = FindProperty("_OutlineScaleWithDistance", properties);
        distanceFadeStartProp = FindProperty("_DistanceFadeStart", properties);
        distanceFadeEndProp = FindProperty("_DistanceFadeEnd", properties);
    }

    protected override void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a Surface Type to unlock its specific settings. This shader variant includes an Inverted Hull outline.", MessageType.Info);

        DrawSurfaceTypeSelector();

        EditorGUILayout.LabelField("Outline Mode", "Inverted Hull");
        if (GUILayout.Button("Remove Outline (Switch to Standard Opaque)"))
        {
            if (EditorUtility.DisplayDialog("Switch Shader?", "This will switch to the standard Opaque shader and remove the outline. Are you sure?", "Yes", "No"))
            {
                SwitchShader("Bill's Toon/Opaque");
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected override void DrawMainProperties()
    {
        base.DrawMainProperties();

        DrawFoldout("Inverted Hull Outline", ref showHullOutlineSettings, () =>
        {
            materialEditor.ShaderProperty(outlineColorProp, "Color");
            materialEditor.ShaderProperty(outlineWidthProp, "Width");
            materialEditor.ShaderProperty(outlineScaleWithDistanceProp, "Screen-Space Scaling");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World-Space Distance Fade", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(distanceFadeStartProp, "Fade Start");
            materialEditor.ShaderProperty(distanceFadeEndProp, "Fade End");
            EditorGUI.indentLevel--;
        });
    }

    protected override void ApplyKeywords()
    {
        base.ApplyKeywords();
        SetKeyword("_OUTLINE_SCALE_WITH_DISTANCE", outlineScaleWithDistanceProp.floatValue > 0.5f);
    }
}