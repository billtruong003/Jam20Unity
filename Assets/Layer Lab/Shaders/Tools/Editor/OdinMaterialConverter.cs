// OdinMaterialConverter.cs (Final Fix)
// Yêu cầu: Phải có Odin Inspector trong project.
// Đặt file này vào trong một thư mục có tên "Editor".

using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Linq;

public class OdinMaterialConverter : OdinEditorWindow
{
    [MenuItem("Tools/Wieldy/Odin Material Converter")]
    private static void OpenWindow()
    {
        GetWindow<OdinMaterialConverter>("Material Converter").Show();
    }

    // -- Dữ liệu chính của công cụ --

    [Title("1. Select Materials to Convert", bold: true)]
    [InfoBox("Kéo và thả các materials từ cửa sổ Project vào danh sách bên dưới.")]
    [Required("Vui lòng chọn ít nhất một material để chuyển đổi.")]
    [AssetsOnly]
    [ListDrawerSettings(NumberOfItemsPerPage = 5, ShowPaging = true, DraggableItems = false)]
    public List<Material> materialsToConvert = new List<Material>();

    [Title("2. Select Target Shader", bold: true)]
    [Required("Phải chọn một Shader đích.")]
    [OnValueChanged(nameof(OnTargetShaderChanged))] // Attribute này sẽ tự động yêu cầu Odin vẽ lại UI
    [ValueDropdown(nameof(GetAllShadersInProject))]
    public Shader targetShader;

    [Title("3. Define Property Mappings", bold: true)]
    [InfoBox("Ánh xạ thuộc tính từ shader cũ sang shader mới. Sử dụng dropdown để đảm bảo chính xác.")]
    [ListDrawerSettings(CustomAddFunction = nameof(CreateNewMapping), DraggableItems = true, ShowFoldout = false)]
    public List<PropertyMapping> propertyMappings = new List<PropertyMapping>();


    // -- Các nút chức năng và thực thi --

    [Button("Add Common Mappings (Standard -> Custom)", ButtonSizes.Large)]
    [GUIColor(0.2f, 0.8f, 0.4f)]
    [BoxGroup("Actions")]
    private void AddDefaultMappings()
    {
        propertyMappings.Clear();
        propertyMappings.Add(new PropertyMapping(this) { SourceName = "_BaseMap", DestinationName = "_MainTex" });
        propertyMappings.Add(new PropertyMapping(this) { SourceName = "_BaseColor", DestinationName = "_Color" });
        propertyMappings.Add(new PropertyMapping(this) { SourceName = "_BumpMap", DestinationName = "_NormalMap" });
        propertyMappings.Add(new PropertyMapping(this) { SourceName = "_MetallicGlossMap", DestinationName = "_MetallicGlossMap" });
        propertyMappings.Add(new PropertyMapping(this) { SourceName = "_Glossiness", DestinationName = "_Glossiness" });
        propertyMappings.Add(new PropertyMapping(this) { SourceName = "_EmissionMap", DestinationName = "_EmissionMap" });
        propertyMappings.Add(new PropertyMapping(this) { SourceName = "_EmissionColor", DestinationName = "_EmissionColor" });
    }

    [Button("CONVERT MATERIALS", ButtonSizes.Gigantic)]
    [GUIColor(0.4f, 0.8f, 1f)]
    [EnableIf(nameof(CanExecute))]
    [BoxGroup("Actions")]
    private void ExecuteConversion()
    {
        if (!CanExecute()) return;

        Undo.RecordObjects(materialsToConvert.ToArray(), "Convert Materials Shader");
        int successCount = 0;

        foreach (Material mat in materialsToConvert)
        {
            if (mat == null) continue;

            Dictionary<string, CachedProperty> oldProperties = CacheMaterialProperties(mat);
            mat.shader = targetShader;
            ApplyCachedProperties(mat, oldProperties);
            EditorUtility.SetDirty(mat);
            successCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Odin Material Converter] Successfully converted {successCount} material(s) to shader '{targetShader.name}'.");
        this.ShowNotification(new GUIContent($"Conversion complete. Processed {successCount} material(s)."));
    }

    // -- Các lớp và phương thức hỗ trợ --

    [System.Serializable]
    public class PropertyMapping
    {
        [ValueDropdown(nameof(GetSourcePropertyNames))]
        [HorizontalGroup("Mapping", Width = 0.5f)]
        [BoxGroup("Mapping/From")]
        public string SourceName;

        [ValueDropdown(nameof(GetDestinationPropertyNames))]
        [HorizontalGroup("Mapping", Width = 0.5f)]
        [BoxGroup("Mapping/To")]
        public string DestinationName;

        [System.NonSerialized]
        private OdinMaterialConverter ownerWindow;

        public PropertyMapping(OdinMaterialConverter owner) => this.ownerWindow = owner;

        private IEnumerable<string> GetSourcePropertyNames() => ownerWindow?.GetSourcePropertyNames() ?? Enumerable.Empty<string>();
        private IEnumerable<string> GetDestinationPropertyNames() => ownerWindow?.GetDestinationPropertyNames() ?? Enumerable.Empty<string>();
    }

    private class CachedProperty
    {
        public object Value { get; }
        public ShaderUtil.ShaderPropertyType Type { get; }
        public CachedProperty(object value, ShaderUtil.ShaderPropertyType type) { Value = value; Type = type; }
    }

    private void CreateNewMapping() => this.propertyMappings.Add(new PropertyMapping(this));

    private bool CanExecute() => targetShader != null && materialsToConvert.Count > 0 && materialsToConvert.All(m => m != null);

    private void OnTargetShaderChanged()
    {
        // SỬA LỖI: Phương thức này được để trống.
        // Chỉ cần sự tồn tại của nó để [OnValueChanged] hoạt động và tự động
        // kích hoạt việc vẽ lại giao diện, cập nhật các dropdown bên dưới.
    }

    // -- Logic lõi --

    private Dictionary<string, CachedProperty> CacheMaterialProperties(Material material)
    {
        var cachedProps = new Dictionary<string, CachedProperty>();
        if (material == null || material.shader == null) return cachedProps;

        for (int i = 0; i < ShaderUtil.GetPropertyCount(material.shader); i++)
        {
            string name = ShaderUtil.GetPropertyName(material.shader, i);
            var type = ShaderUtil.GetPropertyType(material.shader, i);
            object value = null;

            switch (type)
            {
                case ShaderUtil.ShaderPropertyType.Color: value = material.GetColor(name); break;
                case ShaderUtil.ShaderPropertyType.TexEnv: value = material.GetTexture(name); break;
                case ShaderUtil.ShaderPropertyType.Float: case ShaderUtil.ShaderPropertyType.Range: value = material.GetFloat(name); break;
                case ShaderUtil.ShaderPropertyType.Vector: value = material.GetVector(name); break;
            }
            if (value != null && !cachedProps.ContainsKey(name)) cachedProps.Add(name, new CachedProperty(value, type));
        }
        return cachedProps;
    }

    private void ApplyCachedProperties(Material material, Dictionary<string, CachedProperty> cachedProperties)
    {
        foreach (var mapping in propertyMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.SourceName) || string.IsNullOrWhiteSpace(mapping.DestinationName)) continue;

            if (cachedProperties.TryGetValue(mapping.SourceName, out var prop) && material.HasProperty(mapping.DestinationName))
            {
                switch (prop.Type)
                {
                    case ShaderUtil.ShaderPropertyType.Color: material.SetColor(mapping.DestinationName, (Color)prop.Value); break;
                    case ShaderUtil.ShaderPropertyType.TexEnv: material.SetTexture(mapping.DestinationName, (Texture)prop.Value); break;
                    case ShaderUtil.ShaderPropertyType.Float: case ShaderUtil.ShaderPropertyType.Range: material.SetFloat(mapping.DestinationName, (float)prop.Value); break;
                    case ShaderUtil.ShaderPropertyType.Vector: material.SetVector(mapping.DestinationName, (Vector4)prop.Value); break;
                }
            }
        }
    }

    // -- Các phương thức cung cấp dữ liệu cho ValueDropdown của Odin --

    private IEnumerable<ValueDropdownItem<Shader>> GetAllShadersInProject()
    {
        return ShaderUtil.GetAllShaderInfo()
            .Select(info => Shader.Find(info.name))
            .Where(s => s != null && !s.name.StartsWith("Hidden/") && (s.hideFlags & HideFlags.DontSave) == 0)
            .OrderBy(s => s.name)
            .Select(s => new ValueDropdownItem<Shader>(s.name, s));
    }

    private IEnumerable<string> GetSourcePropertyNames()
    {
        if (materialsToConvert == null || materialsToConvert.Count == 0 || materialsToConvert[0] == null)
            return Enumerable.Empty<string>();

        Material sourceMat = materialsToConvert[0];
        return GetPropertyNamesFromShader(sourceMat.shader);
    }

    private IEnumerable<string> GetDestinationPropertyNames()
    {
        if (targetShader == null)
            return Enumerable.Empty<string>();

        return GetPropertyNamesFromShader(targetShader);
    }

    private IEnumerable<string> GetPropertyNamesFromShader(Shader shader)
    {
        if (shader == null) return Enumerable.Empty<string>();

        return Enumerable.Range(0, ShaderUtil.GetPropertyCount(shader))
            .Select(i => ShaderUtil.GetPropertyName(shader, i))
            .OrderBy(name => name);
    }
}