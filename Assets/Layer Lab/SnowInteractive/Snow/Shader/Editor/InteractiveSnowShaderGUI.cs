using UnityEngine;
using UnityEditor;
using System;

public class InteractiveSnowShaderGUI : ShaderGUI
{
    // Biến để lưu trạng thái đóng/mở của các foldout
    private static bool showMainProperties = true;
    private static bool showSnowShape = true;
    private static bool showTessellation = true;
    private static bool showInteractivePath = true;
    private static bool showEffects = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        materialEditor.SetDefaultGUIWidths();

        // Sử dụng BeginFoldoutHeaderGroup để tạo các vùng có thể thu gọn
        showMainProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showMainProperties, "Main Properties");
        if (showMainProperties)
        {
            // Vẽ một hộp xung quanh các thuộc tính để phân vùng rõ ràng
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_SnowColor", "Snow Color", materialEditor, properties);
            DrawProperty("_MainTex", "Snow Texture", materialEditor, properties);
            DrawProperty("_SnowTextureOpacity", "Snow Texture Opacity", materialEditor, properties);
            DrawProperty("_SnowTextureScale", "Snow Texture Scale", materialEditor, properties);
            DrawProperty("_ShadowColor", "Shadow Color", materialEditor, properties);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showSnowShape = EditorGUILayout.BeginFoldoutHeaderGroup(showSnowShape, "Snow Shape");
        if (showSnowShape)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_NoiseTexture", "Snow Noise", materialEditor, properties);
            DrawProperty("_NoiseScale", "Noise Scale", materialEditor, properties);
            DrawProperty("_NoiseWeight", "Noise Weight", materialEditor, properties);
            DrawProperty("_SnowHeight", "Snow Height", materialEditor, properties);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showTessellation = EditorGUILayout.BeginFoldoutHeaderGroup(showTessellation, "Tessellation");
        if (showTessellation)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_TessellationFactor", "Tessellation Factor", materialEditor, properties);
            DrawProperty("_MaxTessellationDistance", "Max Tessellation Distance", materialEditor, properties);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showInteractivePath = EditorGUILayout.BeginFoldoutHeaderGroup(showInteractivePath, "Interactive Path");
        if (showInteractivePath)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_PathColorIn", "Path Inner Color", materialEditor, properties);
            DrawProperty("_PathColorOut", "Path Outer Color", materialEditor, properties);
            DrawProperty("_PathBlending", "Path Blending", materialEditor, properties);
            DrawProperty("_SnowPathStrength", "Path Strength", materialEditor, properties);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showEffects = EditorGUILayout.BeginFoldoutHeaderGroup(showEffects, "Effects");
        if (showEffects)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_SparkleNoise", "Sparkle Noise", materialEditor, properties);
            DrawProperty("_SparkleScale", "Sparkle Scale", materialEditor, properties);
            DrawProperty("_SparkCutoff", "Sparkle Cutoff", materialEditor, properties);
            DrawProperty("_RimColor", "Rim Color", materialEditor, properties);
            DrawProperty("_RimPower", "Rim Power", materialEditor, properties);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawProperty(string propertyName, string label, MaterialEditor editor, MaterialProperty[] properties)
    {
        // Sử dụng FindProperty để tìm thuộc tính từ mảng
        MaterialProperty property = FindProperty(propertyName, properties);
        if (property != null)
        {
            // Dùng GUIContent để hiển thị nhãn thân thiện hơn
            editor.ShaderProperty(property, new GUIContent(label));
        }
    }
}