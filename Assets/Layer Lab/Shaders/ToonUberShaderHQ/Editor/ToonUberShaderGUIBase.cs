using UnityEngine;
using UnityEditor;
using System;

public abstract class ToonUberShaderGUIBase : ShaderGUI
{
    protected MaterialEditor materialEditor;
    protected MaterialProperty[] properties;
    protected Material[] materials;

    private static bool showAdvancedSettings = false;
    private static GUIStyle headerStyle;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        properties = props;
        materials = Array.ConvertAll(editor.targets, item => (Material)item);

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

        DrawHeader();
        DrawWorkflowSettings();

        EditorGUILayout.Space();

        DrawMainProperties();

        EditorGUILayout.Space();

        DrawAdvancedSettings();

        if (EditorGUI.EndChangeCheck())
        {
            ApplyKeywords();
        }
    }

    protected abstract void FindProperties();
    protected abstract void DrawWorkflowSettings();
    protected abstract void DrawMainProperties();
    protected abstract void ApplyKeywords();

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Bill's Toon Shader", headerStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    protected static void DrawFoldout(string title, ref bool state, Action contents)
    {
        var rect = EditorGUILayout.BeginVertical();
        state = EditorGUI.Foldout(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), state, title, true, EditorStyles.foldoutHeader);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        if (state)
        {
            EditorGUILayout.BeginVertical("box");
            contents.Invoke();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(2);
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

    protected void SetKeyword(string keyword, bool state)
    {
        foreach (var mat in materials)
        {
            if (state)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }
    }
}