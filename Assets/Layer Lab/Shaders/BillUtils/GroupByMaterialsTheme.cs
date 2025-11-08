using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

/// <summary>
/// Cung cấp một quy trình làm việc hai bước để tự động tổ chức các model vũ khí.
/// 1. Chọn một loại vũ khí, sau đó đổi tên và nhóm tất cả các model rời rạc theo loại đó.
/// 2. Sắp xếp các vũ khí bên trong mỗi nhóm theo phẩm chất đã định nghĩa.
/// </summary>
[AddComponentMenu("Tools/Weapon Organizer V2")]
public class WeaponOrganizer : MonoBehaviour
{
    [Title("Configuration")]
    [InfoBox("Định nghĩa các loại vũ khí sẽ được dùng để tạo nhóm và trong danh sách chọn lựa.")]
    [SerializeField, ListDrawerSettings]
    private List<string> weaponTypes = new List<string> { "Hammer", "Dagger", "Sword", "Axe", "Shield" };

    [InfoBox("Xếp hạng phẩm chất từ thấp đến cao. Thứ tự này sẽ quyết định vị trí của vũ khí trong nhóm sau khi sắp xếp.")]
    [SerializeField, ListDrawerSettings]
    private List<string> qualityTiers = new List<string> { "Wood", "Stone", "bronze", "Hard", "Sharp", "Ice", "Poison", "Gold", "Storm", "Elf", "Rune", "Skull", "Crazy", "Darkness", "Abyss", "pirate", "Dark" };


    [Title("Step 1: Rename & Group")]
    [InfoBox("Chọn loại vũ khí để áp dụng cho tất cả các đối tượng chưa được nhóm bên dưới.")]
    [ValueDropdown("weaponTypes")]
    [Required("Bạn phải chọn một loại vũ khí.")]
    [SerializeField]
    private string selectedWeaponType;

    [Button("1. Rename and Group by Selected Type", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void RenameAndGroupWeapons()
    {
        var looseObjects = GetLooseChildObjects();
        if (looseObjects.Count == 0)
        {
            Debug.Log("Không tìm thấy đối tượng nào chưa được nhóm để xử lý.");
            return;
        }

        Transform groupContainer = FindOrCreateGroupContainer(selectedWeaponType);
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        foreach (var weaponObject in looseObjects)
        {
            string theme = GetThemeFromMaterial(weaponObject);
            if (string.IsNullOrEmpty(theme))
            {
                Debug.LogWarning($"Bỏ qua '{weaponObject.name}' vì không thể xác định Theme từ material.", weaponObject);
                continue;
            }

            string formattedTheme = textInfo.ToTitleCase(theme.ToLower());
            weaponObject.name = $"{formattedTheme}_{selectedWeaponType}";
            weaponObject.SetParent(groupContainer, false);
        }

        Debug.Log($"Đã xử lý và nhóm {looseObjects.Count} đối tượng vào nhóm '{selectedWeaponType}'.");
    }


    [Title("Step 2: Sort")]
    [Button("2. Sort All Groups by Quality", ButtonSizes.Large), GUIColor(1f, 0.9f, 0.6f)]
    private void SortAllGroupsByQuality()
    {
        var qualityRankMap = CreateQualityRankMap();
        var allGroupContainers = GetAllGroupContainers();

        if (!allGroupContainers.Any())
        {
            Debug.LogWarning("Không tìm thấy nhóm vũ khí nào để sắp xếp. Hãy chạy bước 1 trước.");
            return;
        }

        foreach (var group in allGroupContainers)
        {
            var weaponsInGroup = GetChildrenAsList(group);

            var sortedWeapons = weaponsInGroup.OrderBy(weapon =>
            {
                string theme = GetThemeFromObjectName(weapon.name);
                return qualityRankMap.TryGetValue(theme.ToLower(), out int rank) ? rank : int.MaxValue;
            }).ToList();

            ApplyHierarchyOrder(sortedWeapons);
        }

        Debug.Log("Hoàn tất sắp xếp tất cả các nhóm theo phẩm chất.");
    }


    #region Helper Methods

    private List<Transform> GetLooseChildObjects()
    {
        var looseObjects = new List<Transform>();
        var groupNames = new HashSet<string>(weaponTypes, System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!groupNames.Contains(child.name))
            {
                looseObjects.Add(child);
            }
        }
        return looseObjects;
    }

    private IEnumerable<Transform> GetAllGroupContainers()
    {
        var groupNames = new HashSet<string>(weaponTypes, System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (groupNames.Contains(child.name))
            {
                yield return child;
            }
        }
    }

    private Transform FindOrCreateGroupContainer(string typeName)
    {
        Transform container = transform.Find(typeName);
        if (container == null)
        {
            container = new GameObject(typeName).transform;
            container.SetParent(transform, false);
        }
        return container;
    }

    private string GetThemeFromMaterial(Transform target)
    {
        var renderer = target.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null || string.IsNullOrEmpty(renderer.sharedMaterial.name))
        {
            return null;
        }
        return renderer.sharedMaterial.name.Split('_')[0];
    }

    private Dictionary<string, int> CreateQualityRankMap()
    {
        return qualityTiers
            .Select((tier, index) => new { tier, index })
            .ToDictionary(item => item.tier.ToLower(), item => item.index);
    }

    private string GetThemeFromObjectName(string objectName)
    {
        int separatorIndex = objectName.IndexOf('_');
        if (separatorIndex == -1) return string.Empty;
        return objectName.Substring(0, separatorIndex);
    }

    private List<Transform> GetChildrenAsList(Transform parent)
    {
        var list = new List<Transform>(parent.childCount);
        foreach (Transform child in parent)
        {
            list.Add(child);
        }
        return list;
    }

    private void ApplyHierarchyOrder(List<Transform> sortedObjects)
    {
        for (int i = 0; i < sortedObjects.Count; i++)
        {
            sortedObjects[i].SetSiblingIndex(i);
        }
    }

    #endregion
}