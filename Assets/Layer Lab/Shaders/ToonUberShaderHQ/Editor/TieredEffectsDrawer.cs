using UnityEngine;
using UnityEditor;

public static class TieredEffectsDrawer
{
    public enum EffectType
    {
        None,
        [InspectorName("Rare - Pulsing Glow")] Rare_PulsingGlow,
        [InspectorName("Rare - Sparkles")] Rare_Sparkles,
        [InspectorName("Epic - Fire Aura")] Epic_FireAura,
        [InspectorName("Epic - Electric Field")] Epic_ElectricField,
        [InspectorName("Legendary - Cosmic Rift")] Legendary_CosmicRift,
        [InspectorName("Legendary - Holy Aura")] Legendary_HolyAura
    }

    public static void DrawTieredEffectsSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        editor.ShaderProperty(props.effectType, "Effect Type");
        if (props.effectType.hasMixedValue) return;

        EditorGUI.indentLevel++;
        var currentType = (EffectType)props.effectType.floatValue;
        switch (currentType)
        {
            case EffectType.Rare_PulsingGlow:
                DrawPulsingGlowSettings(editor, props);
                break;
            case EffectType.Rare_Sparkles:
                DrawSparklesSettings(editor, props);
                break;
            case EffectType.Epic_FireAura:
                DrawFireAuraSettings(editor, props);
                break;
            case EffectType.Epic_ElectricField:
                DrawElectricFieldSettings(editor, props);
                break;
            case EffectType.Legendary_CosmicRift:
                DrawCosmicRiftSettings(editor, props);
                break;
            case EffectType.Legendary_HolyAura:
                DrawHolyAuraSettings(editor, props);
                break;
        }
        EditorGUI.indentLevel--;
    }

    private static void DrawPulsingGlowSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        EditorGUILayout.LabelField("Pulsing Glow", EditorStyles.miniBoldLabel);
        editor.ShaderProperty(props.rareColor1, "Glow Color");
        editor.ShaderProperty(props.rareFloat1, "Pulse Speed");
        editor.ShaderProperty(props.rareFloat2, "Pulse Intensity");
    }

    private static void DrawSparklesSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        EditorGUILayout.LabelField("Sparkles", EditorStyles.miniBoldLabel);
        editor.ShaderProperty(props.rareColor1, "Sparkle Color");
        editor.ShaderProperty(props.rareFloat1, "Sparkle Density");
        editor.ShaderProperty(props.rareFloat2, "Flicker Speed");
    }

    private static void DrawFireAuraSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        EditorGUILayout.LabelField("Fire Aura", EditorStyles.miniBoldLabel);
        editor.TexturePropertySingleLine(new GUIContent("Noise Texture (R)"), props.effectTex1, props.epicColor1);
        editor.ShaderProperty(props.epicColor2, "Aura Color 2");
        editor.ShaderProperty(props.epicFloat1, "Scroll Speed");
        editor.ShaderProperty(props.epicFloat2, "Noise Scale");
        editor.ShaderProperty(props.epicFloat3, "Fresnel Power");
    }

    private static void DrawElectricFieldSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        EditorGUILayout.LabelField("Electric Field", EditorStyles.miniBoldLabel);
        editor.TexturePropertySingleLine(new GUIContent("Line Pattern (R)"), props.effectTex1, props.epicColor1);
        editor.ShaderProperty(props.epicFloat1, "Scroll Speed");
        editor.ShaderProperty(props.epicFloat2, "Line Density");
        editor.ShaderProperty(props.epicFloat3, "Jitter Intensity");
    }

    private static void DrawCosmicRiftSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        EditorGUILayout.LabelField("Cosmic Rift", EditorStyles.miniBoldLabel);
        editor.TexturePropertySingleLine(new GUIContent("Nebula Texture (R)"), props.effectTex1, props.legendaryColor1);
        editor.ShaderProperty(props.legendaryColor2, "Rift Edge Color");
        editor.ShaderProperty(props.legendaryFloat1, "Scroll Speed");
        editor.ShaderProperty(props.legendaryFloat2, "Nebula Scale");
        editor.ShaderProperty(props.legendaryFloat3, "Rift Sharpness");
    }

    private static void DrawHolyAuraSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        EditorGUILayout.LabelField("Holy Aura", EditorStyles.miniBoldLabel);
        editor.ShaderProperty(props.legendaryColor1, "Aura Color");
        editor.ShaderProperty(props.legendaryFloat1, "Wave Speed");
        editor.ShaderProperty(props.legendaryFloat2, "Wave Frequency");
        editor.ShaderProperty(props.legendaryFloat3, "Fresnel Power");
    }
}