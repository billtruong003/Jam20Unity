using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Audio;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class DataOptimizer : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    [MenuItem("Tools/Optimize & Compress Data")]
    public static void OptimizeAllData()
    {
        CompressTextures();
        OptimizeAudioClips();
        ConfigureAssetBundles();
        OptimizePlayerSettings();
        CleanUnusedAssets();
        Debug.Log("✅ Data optimization completed!");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        OptimizeAllData(); // Tự động chạy khi build
    }

    static void CompressTextures()
    {
        string[] textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int count = 0;

        foreach (string guid in textureGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null) continue;

            // LẤY IMPORTER AN TOÀN
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null) continue;

            // KIỂM TRA CHÍNH XÁC LOẠI
            if (!(importer is TextureImporter textureImporter))
            {
                // Có thể là Sprite, hoặc loại khác → bỏ qua hoặc xử lý riêng
                continue;
            }

            bool changed = false;

            // === Tối ưu Android ===
            TextureImporterPlatformSettings androidSettings = textureImporter.GetPlatformTextureSettings("Android");
            if (!androidSettings.overridden)
            {
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = Mathf.Min(2048, texture.width);
                androidSettings.format = TextureImporterFormat.ASTC_6x6;
                androidSettings.compressionQuality = (int)TextureCompressionQuality.Best;
                textureImporter.SetPlatformTextureSettings(androidSettings);
                changed = true;
            }

            // === Tối ưu iOS ===
            TextureImporterPlatformSettings iOSSettings = textureImporter.GetPlatformTextureSettings("iOS");
            if (!iOSSettings.overridden)
            {
                iOSSettings.overridden = true;
                iOSSettings.maxTextureSize = Mathf.Min(2048, texture.width);
                iOSSettings.format = TextureImporterFormat.ASTC_6x6;
                iOSSettings.compressionQuality = (int)TextureCompressionQuality.Best;
                textureImporter.SetPlatformTextureSettings(iOSSettings);
                changed = true;
            }

            // === Default Settings ===
            if (!textureImporter.isReadable && textureImporter.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                textureImporter.mipmapEnabled = texture.width > 256;
                changed = true;
            }

            if (changed)
            {
                textureImporter.SaveAndReimport();
                count++;
            }
        }

        Debug.Log($"Compressed {count} textures with ASTC.");
    }

    // 2. Nén Audio
    static void OptimizeAudioClips()
    {
        string[] audioGUIDs = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        int count = 0;

        foreach (string guid in audioGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;

            bool changed = false;

            // Android: Vorbis
            AudioImporterSampleSettings android = importer.GetOverrideSampleSettings("Android");
            if (android.loadType != AudioClipLoadType.CompressedInMemory || android.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                android.loadType = AudioClipLoadType.CompressedInMemory;
                android.compressionFormat = AudioCompressionFormat.Vorbis;
                android.quality = 0.6f;
                importer.SetOverrideSampleSettings("Android", android);
                changed = true;
            }

            // iOS: MP3 or AAC
            AudioImporterSampleSettings ios = importer.GetOverrideSampleSettings("iOS");
            if (ios.loadType != AudioClipLoadType.CompressedInMemory || ios.compressionFormat != AudioCompressionFormat.MP3)
            {
                ios.loadType = AudioClipLoadType.CompressedInMemory;
                ios.compressionFormat = AudioCompressionFormat.MP3;
                ios.quality = 0.6f;
                importer.SetOverrideSampleSettings("iOS", ios);
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                count++;
            }
        }

        Debug.Log($"🎵 Optimized {count} audio clips.");
    }

    // 3. Cấu hình AssetBundle (nén LZ4/LZMA)
    static void ConfigureAssetBundles()
    {
        BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression; // LZ4
        // Hoặc dùng: BuildAssetBundleOptions.UncompressedAssetBundle nếu cần debug

        // Ví dụ: Build tất cả AssetBundle
        string outputPath = "Assets/AssetBundles";
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        // Build pipeline (có thể tùy chỉnh)
        BuildPipeline.BuildAssetBundles(outputPath, options, EditorUserBuildSettings.activeBuildTarget);
        Debug.Log($"📦 AssetBundles built with LZ4 compression at {outputPath}");
    }

    // 4. Tối ưu Player Settings
    static void OptimizePlayerSettings()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);

        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.High);

        EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
        Debug.Log("⚙️ Player Settings optimized for release.");
    }

    // 5. Dọn dẹp tài nguyên không dùng
    static void CleanUnusedAssets()
    {
        // Xóa file tạm, log, cache
        string[] tempFolders = { "Library", "Temp", "Obj", "Build", "Logs" };
        foreach (string folder in tempFolders)
        {
            string path = Path.Combine(Application.dataPath, "..", folder);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                File.Delete(path + ".meta");
            }
        }
        Debug.Log("🧹 Cleaned temp folders.");
    }
}