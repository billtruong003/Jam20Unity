using UnityEngine;
using UnityEditor;
using System;

public static class ToonOpaqueDrawerUtils
{
    public enum SurfaceType { Opaque, Metallic, Foliage, Bling }

    private static bool showToonSettings = true;
    private static bool showMetallicSettings = true;
    private static bool showFoliageSettings = true;
    private static bool showBlingSettings = true;

    private static void DrawFoldout(string title, ref bool state, Action contents)
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

    public static void DrawToonSettings(MaterialEditor editor, MaterialProperty toonStyle, MaterialProperty shadowThreshold, MaterialProperty midtoneThreshold, MaterialProperty smoothness, MaterialProperty shadowTint, MaterialProperty midtoneColor)
    {
        DrawFoldout("Toon Shading", ref showToonSettings, () =>
        {
            editor.ShaderProperty(toonStyle, "Style");
            editor.ShaderProperty(shadowThreshold, "Shadow Threshold");
            editor.ShaderProperty(midtoneThreshold, "Mid-tone Threshold");

            bool isHardStyle = toonStyle.floatValue > 0.5f;

            EditorGUI.BeginDisabledGroup(isHardStyle && !toonStyle.hasMixedValue);
            editor.ShaderProperty(smoothness, "Ramp Smoothness");
            EditorGUI.EndDisabledGroup();

            editor.ShaderProperty(shadowTint, "Shadow Tint");
            editor.ShaderProperty(midtoneColor, "Mid-tone Color");

            if (!toonStyle.hasMixedValue)
            {
                if (isHardStyle)
                {
                    EditorGUILayout.HelpBox("Hard Style creates sharp, distinct bands. 'Ramp Smoothness' is disabled in this mode.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Smooth Style uses 'Ramp Smoothness' to blend between light and shadow bands.", MessageType.Info);
                }
            }
        });
    }

    public static void DrawMetallicSettings(MaterialEditor editor, MaterialProperty ramp, MaterialProperty brightness, MaterialProperty offset, MaterialProperty specColor, MaterialProperty hiOffset, MaterialProperty hiColor, MaterialProperty rimColor, MaterialProperty rimPower)
    {
        DrawFoldout("Stylized Metal", ref showMetallicSettings, () =>
        {
            editor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), ramp);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
            editor.ShaderProperty(brightness, "Brightness");
            editor.ShaderProperty(offset, "Size");
            editor.ShaderProperty(specColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Highlight", EditorStyles.boldLabel);
            editor.ShaderProperty(hiOffset, "Size");
            editor.ShaderProperty(hiColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rim Light", EditorStyles.boldLabel);
            editor.ShaderProperty(rimColor, "Color");
            editor.ShaderProperty(rimPower, "Power");
        });
    }

    public static void DrawFoliageSettings(MaterialEditor editor, MaterialProperty windFreq, MaterialProperty windAmp, MaterialProperty windDir, MaterialProperty transColor, MaterialProperty transStrength)
    {
        DrawFoldout("Foliage", ref showFoliageSettings, () =>
        {
            EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
            editor.ShaderProperty(windFreq, "Frequency");
            editor.ShaderProperty(windAmp, "Amplitude");
            editor.ShaderProperty(windDir, "Direction");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            editor.ShaderProperty(transColor, "Translucency Color");
            editor.ShaderProperty(transStrength, "Translucency Strength");
        });
    }

    public static void DrawBlingSettings(MaterialEditor editor, MaterialProperty noiseTex, MaterialProperty worldSpace, MaterialProperty color, MaterialProperty intensity, MaterialProperty scale, MaterialProperty speed, MaterialProperty fresnelPower, MaterialProperty threshold)
    {
        DrawFoldout("Bling Effect", ref showBlingSettings, () =>
        {
            EditorGUILayout.HelpBox("This effect uses an optimized texture-based method for sparkling.", MessageType.Info);
            editor.TexturePropertySingleLine(new GUIContent(noiseTex.displayName), noiseTex);
            EditorGUILayout.Space();

            editor.ShaderProperty(worldSpace, worldSpace.displayName);
            editor.ShaderProperty(color, color.displayName);
            editor.ShaderProperty(intensity, intensity.displayName);
            editor.ShaderProperty(scale, scale.displayName);
            editor.ShaderProperty(speed, speed.displayName);
            editor.ShaderProperty(fresnelPower, fresnelPower.displayName);
            editor.ShaderProperty(threshold, threshold.displayName);
        });
    }
}