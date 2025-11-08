using UnityEngine;
using UnityEditor;
using System;

public static class ToonOpaqueUIDrawer
{
    public enum SurfaceType { Opaque, Metallic, Foliage, Bling, Cosmic }
    public enum DissolveType { Noise = 0, Linear = 1, Radial = 2, Pattern = 3, AlphaBlend = 4, Shatter = 5 }

    public static void DrawToonSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.ToonShadingProps props)
    {
        EditorGUILayout.LabelField("Main Light", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Controls a three-step lighting ramp: Shadow -> Mid-tone -> Highlight for the main directional light.", MessageType.Info);
        editor.ShaderProperty(props.shadowTint, props.shadowTint.displayName);
        editor.ShaderProperty(props.midtoneColor, props.midtoneColor.displayName);
        editor.ShaderProperty(props.shadowThreshold, props.shadowThreshold.displayName);
        editor.ShaderProperty(props.midtoneThreshold, props.midtoneThreshold.displayName);
        editor.ShaderProperty(props.rampSmoothness, props.rampSmoothness.displayName);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Additional Lights (Point/Spot)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Separate controls for additional lights to prevent oversaturation and achieve a better toon look.", MessageType.Info);
        editor.ShaderProperty(props.addLightShadowTint, props.addLightShadowTint.displayName);
        editor.ShaderProperty(props.addLightMidtoneColor, props.addLightMidtoneColor.displayName);
        editor.ShaderProperty(props.addLightShadowThreshold, props.addLightShadowThreshold.displayName);
        editor.ShaderProperty(props.addLightMidtoneThreshold, props.addLightMidtoneThreshold.displayName);
        editor.ShaderProperty(props.addLightRampSmoothness, props.addLightRampSmoothness.displayName);
    }

    public static void DrawMetallicSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.MetallicProps props)
    {
        editor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), props.ramp);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
        editor.ShaderProperty(props.brightness, "Brightness");
        editor.ShaderProperty(props.offset, "Size");
        editor.ShaderProperty(props.specColor, "Color");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Highlight", EditorStyles.boldLabel);
        editor.ShaderProperty(props.hiOffset, "Size");
        editor.ShaderProperty(props.hiColor, "Color");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rim Light", EditorStyles.boldLabel);
        editor.ShaderProperty(props.rimColor, "Color");
        editor.ShaderProperty(props.rimPower, "Power");
    }

    public static void DrawFoliageSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.FoliageProps props)
    {
        EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
        editor.ShaderProperty(props.windFreq, "Frequency");
        editor.ShaderProperty(props.windAmp, "Amplitude");
        editor.ShaderProperty(props.windDir, "Direction");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
        editor.ShaderProperty(props.transColor, "Translucency Color");
        editor.ShaderProperty(props.transStrength, "Translucency Strength");
    }

    public static void DrawBlingSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.BlingProps props)
    {
        editor.ShaderProperty(props.worldSpace, props.worldSpace.displayName);
        editor.ShaderProperty(props.color, props.color.displayName);
        editor.ShaderProperty(props.intensity, props.intensity.displayName);
        editor.ShaderProperty(props.scale, props.scale.displayName);
        editor.ShaderProperty(props.speed, props.speed.displayName);
        editor.ShaderProperty(props.fresnelPower, props.fresnelPower.displayName);
        editor.ShaderProperty(props.threshold, props.threshold.displayName);
    }

    public static void DrawCosmicSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.CosmicProps cosmic, ToonOpaqueShaderGUI_HQ.MetallicProps metallic)
    {
        DrawCosmicInternalLayers(editor, cosmic);
        EditorGUILayout.Space();
        DrawCosmicMappingAndColor(editor, cosmic);
        EditorGUILayout.Space();
        DrawCosmicSurfaceLighting(editor, metallic);
    }

    private static void DrawCosmicInternalLayers(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.CosmicProps props)
    {
        EditorGUILayout.LabelField("Internal Layers (Triplanar)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("These layers have parallax to create depth. Use grayscale noise, dust or star textures.", MessageType.Info);

        EditorGUILayout.LabelField("Nebula Layer 1", EditorStyles.miniBoldLabel);
        editor.TexturePropertySingleLine(new GUIContent(props.tex1.displayName), props.tex1, props.color1);
        editor.ShaderProperty(props.scale1, props.scale1.displayName);
        editor.ShaderProperty(props.speed1, props.speed1.displayName);
        editor.ShaderProperty(props.parallax1, props.parallax1.displayName);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Nebula Layer 2", EditorStyles.miniBoldLabel);
        editor.TexturePropertySingleLine(new GUIContent(props.tex2.displayName), props.tex2, props.color2);
        editor.ShaderProperty(props.scale2, props.scale2.displayName);
        editor.ShaderProperty(props.speed2, props.speed2.displayName);
        editor.ShaderProperty(props.parallax2, props.parallax2.displayName);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Star Field Layer", EditorStyles.miniBoldLabel);
        editor.TexturePropertySingleLine(new GUIContent(props.starTex.displayName), props.starTex, props.starColor);
        editor.ShaderProperty(props.starScale, props.starScale.displayName);
        editor.ShaderProperty(props.starSpeed, props.starSpeed.displayName);
        editor.ShaderProperty(props.starParallax, props.starParallax.displayName);
    }

    private static void DrawCosmicMappingAndColor(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.CosmicProps props)
    {
        EditorGUILayout.LabelField("Mapping & Color", EditorStyles.boldLabel);
        editor.ShaderProperty(props.triplanarSharpness, props.triplanarSharpness.displayName);
        editor.ShaderProperty(props.ambientColor, props.ambientColor.displayName);
        EditorGUILayout.HelpBox("Use the Alpha channel to blend between Scene Ambient (A=0) and this custom color (A=1).", MessageType.Info);
    }

    private static void DrawCosmicSurfaceLighting(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.MetallicProps props)
    {
        EditorGUILayout.LabelField("External Surface Lighting (Uses Metallic Properties)", EditorStyles.boldLabel);
        editor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), props.ramp);
        editor.ShaderProperty(props.brightness, "Brightness");
        editor.ShaderProperty(props.offset, "Specular Size");
        editor.ShaderProperty(props.specColor, "Specular Color");
        editor.ShaderProperty(props.hiOffset, "Highlight Size");
        editor.ShaderProperty(props.hiColor, "Highlight Color");
        editor.ShaderProperty(props.rimColor, "Rim Color");
        editor.ShaderProperty(props.rimPower, "Rim Power");
    }

    public static void DrawDissolveSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.DissolveProps props)
    {
        editor.ShaderProperty(props.enableDissolve, "Enable Dissolve Effect");
        if (props.enableDissolve.floatValue < 0.5f) return;

        EditorGUILayout.Space();
        DrawHologramReveal(editor, props);
        EditorGUILayout.Space();
        DrawDissolveControl(editor, props);
        EditorGUILayout.Space();
        DrawDissolveEdge(editor, props);
        EditorGUILayout.Space();
        DrawDissolveVertexEffects(editor, props);
    }

    private static void DrawHologramReveal(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.DissolveProps props)
    {
        editor.ShaderProperty(props.enableHologramReveal, "Enable Hologram Reveal Mode");
        if (props.enableHologramReveal.floatValue > 0.5f)
        {
            EditorGUILayout.HelpBox("Progress controls the effect: [0 to 1] reveals hologram, [1 to 2] materializes object.", MessageType.Info);
            editor.ShaderProperty(props.revealProgress, "Progress");
            editor.TexturePropertySingleLine(new GUIContent(props.hologramPatternTex.displayName), props.hologramPatternTex, props.hologramEmissionColor);
            editor.ShaderProperty(props.hologramPatternScale, "Pattern Scale", 1);
            editor.ShaderProperty(props.hologramFlickerSpeed, "Flicker Speed", 1);
        }
    }

    private static void DrawDissolveControl(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.DissolveProps props)
    {
        EditorGUILayout.LabelField("Dissolve Control", EditorStyles.boldLabel);
        if (props.enableHologramReveal.floatValue < 0.5f)
        {
            editor.ShaderProperty(props.dissolveThreshold, "Threshold");
        }
        editor.ShaderProperty(props.dissolveType, "Type");

        DissolveType currentType = (DissolveType)props.dissolveType.floatValue;
        EditorGUI.indentLevel++;
        switch (currentType)
        {
            case DissolveType.Radial:
                editor.ShaderProperty(props.dissolveDirection, "Center (Offset)");
                editor.ShaderProperty(props.maxDissolveDistance, "Radius");
                editor.ShaderProperty(props.radialDirection, "Direction");
                EditorGUILayout.HelpBox("Direction: 1 for Inside-out, -1 for Outside-in.", MessageType.None);
                break;
            case DissolveType.Linear:
            case DissolveType.Shatter:
                editor.ShaderProperty(props.dissolveDirection, "Direction");
                break;
            case DissolveType.Pattern:
                editor.ShaderProperty(props.patternType, "Pattern");
                editor.ShaderProperty(props.patternFrequency, "Frequency");
                break;
            case DissolveType.AlphaBlend:
                editor.ShaderProperty(props.alphaFadeRange, "Fade Range");
                break;
        }
        EditorGUI.indentLevel--;

        editor.ShaderProperty(props.useTimeAnimation, "Animate Threshold");
        if (props.useTimeAnimation.floatValue > 0.5f) editor.ShaderProperty(props.timeScale, "Animation Speed", 1);
        editor.ShaderProperty(props.useLocalSpace, "Use Local Space Coords");
    }

    private static void DrawDissolveEdge(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.DissolveProps props)
    {
        EditorGUILayout.LabelField("Edge Appearance (Shared)", EditorStyles.boldLabel);
        editor.TexturePropertySingleLine(new GUIContent(props.noiseTex.displayName), props.noiseTex);
        editor.ShaderProperty(props.noiseScale, props.noiseScale.displayName);
        editor.ShaderProperty(props.noiseStrength, props.noiseStrength.displayName);
        editor.ShaderProperty(props.dissolveEdgeWidth, props.dissolveEdgeWidth.displayName);
        editor.ShaderProperty(props.dissolveEdgeColor, props.dissolveEdgeColor.displayName);
    }

    private static void DrawDissolveVertexEffects(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.DissolveProps props)
    {
        DissolveType currentType = (DissolveType)props.dissolveType.floatValue;
        EditorGUILayout.LabelField("Vertex Effects", EditorStyles.boldLabel);

        if (currentType != DissolveType.Shatter)
            editor.ShaderProperty(props.enableVertexDisplacement, props.enableVertexDisplacement.displayName);

        editor.ShaderProperty(props.enableShatterEffect, props.enableShatterEffect.displayName);

        EditorGUI.indentLevel++;
        if (props.enableVertexDisplacement.floatValue > 0.5f && currentType != DissolveType.Shatter)
        {
            editor.ShaderProperty(props.useSaturateDisplacement, "Saturate (Sustained)");
            editor.ShaderProperty(props.vertexDisplacement, "Intensity");
            editor.ShaderProperty(props.bounceWaveWidth, "Effect Width");
        }
        if (props.enableShatterEffect.floatValue > 0.5f)
        {
            editor.ShaderProperty(props.vertexDisplacement, "Outward Push");
            editor.ShaderProperty(props.shatterStrength, "Overall Strength");
            editor.ShaderProperty(props.shatterLiftSpeed, "Lift Speed");
            editor.ShaderProperty(props.shatterOffsetStrength, "Offset Strength");
            editor.ShaderProperty(props.shatterTriggerRange, "Trigger Range");
        }
        EditorGUI.indentLevel--;
    }
}