using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

public class ModularCharacterGeneratorWindow : OdinEditorWindow
{
    [Title("Source Prefab")]
    [Required, AssetsOnly]
    [InfoBox("Kéo prefab gốc có cấu trúc Body, Bone, Parts vào đây.")]
    public GameObject SourcePrefab;

    [Title("Output Configuration")]
    [FolderPath(AbsolutePath = false), Required]
    public string OutputPath = "Assets/GeneratedCharacter";

    private const string BODY_IDENTIFIER = "Body";
    private const string BONE_IDENTIFIER = "Bone";
    private const string PARTS_IDENTIFIER = "Parts";

    [MenuItem("Tools/Modular Character Generator")]
    private static void OpenWindow()
    {
        GetWindow<ModularCharacterGeneratorWindow>().Show();
    }

    [Button(ButtonSizes.Large, Name = "Generate Modular Data")]
    [GUIColor(0.4f, 0.8f, 1f)]
    private void Generate()
    {
        if (!ValidateSourcePrefab()) return;

        string prefabRootPath = Path.Combine(OutputPath, SourcePrefab.name);
        Directory.CreateDirectory(prefabRootPath);

        CharacterModularData data = CreateScriptableObject(prefabRootPath);
        data.BodySets.Clear();
        data.Categories.Clear();

        try
        {
            EditorUtility.DisplayProgressBar("Generating Modular Data", "Initializing...", 0.0f);

            ProcessBodySets(data, prefabRootPath);
            EditorUtility.DisplayProgressBar("Generating Modular Data", "Processing Skeleton...", 0.3f);

            ProcessSkeleton(data, prefabRootPath);
            EditorUtility.DisplayProgressBar("Generating Modular Data", "Processing Parts...", 0.6f);

            ProcessParts(data, prefabRootPath);
            EditorUtility.DisplayProgressBar("Generating Modular Data", "Finalizing...", 0.9f);
        }
        finally
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("Success", $"Successfully generated modular data for {SourcePrefab.name} at {prefabRootPath}", "OK");
        Selection.activeObject = data;
    }

    private bool ValidateSourcePrefab()
    {
        if (SourcePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Source Prefab cannot be null.", "OK");
            return false;
        }

        bool hasBody = SourcePrefab.transform.Find(BODY_IDENTIFIER) != null;
        bool hasBone = SourcePrefab.transform.Find(BONE_IDENTIFIER) != null;
        bool hasParts = SourcePrefab.transform.Find(PARTS_IDENTIFIER) != null;

        if (!hasBody || !hasBone || !hasParts)
        {
            EditorUtility.DisplayDialog("Error", $"Source Prefab is missing required children. It must contain '{BODY_IDENTIFIER}', '{BONE_IDENTIFIER}', and '{PARTS_IDENTIFIER}'.", "OK");
            return false;
        }
        return true;
    }

    private CharacterModularData CreateScriptableObject(string rootPath)
    {
        string dataPath = Path.Combine(rootPath, $"{SourcePrefab.name}_Data.asset");
        CharacterModularData data = AssetDatabase.LoadAssetAtPath<CharacterModularData>(dataPath);
        if (data == null)
        {
            data = CreateInstance<CharacterModularData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }
        return data;
    }

    private void ProcessBodySets(CharacterModularData data, string rootPath)
    {
        Transform bodyRoot = SourcePrefab.transform.Find(BODY_IDENTIFIER);
        string bodyPath = Path.Combine(rootPath, "BodySets");
        Directory.CreateDirectory(bodyPath);

        foreach (Transform bodySetTransform in bodyRoot)
        {
            BodySet newBodySet = new BodySet { Name = bodySetTransform.name };
            var renderers = bodySetTransform.GetComponentsInChildren<Renderer>(true)
                .Where(r => r is SkinnedMeshRenderer || r is MeshRenderer);

            foreach (var renderer in renderers)
            {
                GameObject partPrefab = CreatePrefabFromObject(renderer.gameObject, bodyPath, $"{bodySetTransform.name}_{renderer.name}");
                newBodySet.BodyParts.Add(new PartVariation { Name = renderer.name, Prefab = partPrefab });
            }

            if (newBodySet.BodyParts.Count > 0)
            {
                data.BodySets.Add(newBodySet);
            }
        }
    }

    private void ProcessSkeleton(CharacterModularData data, string rootPath)
    {
        Transform boneRoot = SourcePrefab.transform.Find(BONE_IDENTIFIER);
        data.SkeletonRoot = CreatePrefabFromObject(boneRoot.gameObject, rootPath, "Skeleton").transform;
    }

    private void ProcessParts(CharacterModularData data, string rootPath)
    {
        Transform partsRoot = SourcePrefab.transform.Find(PARTS_IDENTIFIER);
        string partsPath = Path.Combine(rootPath, "Parts");
        Directory.CreateDirectory(partsPath);

        foreach (Transform categoryTransform in partsRoot)
        {
            PartCategory newCategory = new PartCategory { Name = categoryTransform.name };
            var groupedFolders = new Dictionary<string, List<Transform>>();

            // Gom nhóm các thư mục con theo tiền tố
            foreach (Transform groupCandidate in categoryTransform)
            {
                string groupName = groupCandidate.name;
                int lastUnderscore = groupName.LastIndexOf('_');
                string baseName = (lastUnderscore != -1) ? groupName.Substring(0, lastUnderscore) : groupName;

                if (!groupedFolders.ContainsKey(baseName))
                {
                    groupedFolders[baseName] = new List<Transform>();
                }
                groupedFolders[baseName].Add(groupCandidate);
            }

            // Tạo PartGroup và PartVariation từ các nhóm đã gom
            foreach (var pair in groupedFolders)
            {
                PartGroup newGroup = new PartGroup { Name = pair.Key };
                foreach (Transform itemFolder in pair.Value)
                {
                    var renderers = itemFolder.GetComponentsInChildren<Renderer>(true)
                        .Where(r => r is SkinnedMeshRenderer || r is MeshRenderer);

                    foreach (var renderer in renderers)
                    {
                        GameObject variationPrefab = CreatePrefabFromObject(renderer.gameObject, partsPath, renderer.name);
                        // Tên variation sẽ là tên của object renderer
                        newGroup.Variations.Add(new PartVariation { Name = renderer.name, Prefab = variationPrefab });
                    }
                }
                if (newGroup.Variations.Count > 0)
                {
                    newCategory.Groups.Add(newGroup);
                }
            }

            if (newCategory.Groups.Count > 0)
            {
                data.Categories.Add(newCategory);
            }
        }
    }

    private GameObject CreatePrefabFromObject(GameObject sourceObject, string path, string prefabName)
    {
        string sanitizedName = string.Join("_", prefabName.Split(Path.GetInvalidFileNameChars()));
        string fullPath = Path.Combine(path, $"{sanitizedName}.prefab");
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        GameObject instance = Instantiate(sourceObject);
        instance.name = sourceObject.name;

        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, fullPath);
        DestroyImmediate(instance);

        return newPrefab;
    }
}