using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[System.Serializable]
public class PartSelection
{
    [ValueDropdown("GetVariationNames")]
    [OnValueChanged("OnSelectionChanged")]
    public string SelectedVariation;

    [HideInInspector] public string GroupName;
    [HideInInspector] public CharacterModularApplier Applier;

    private IEnumerable<string> GetVariationNames() => Applier?.GetVariationsForGroup(GroupName) ?? Enumerable.Empty<string>();
    private void OnSelectionChanged() => Applier?.EquipPart(GroupName, SelectedVariation);
}

[System.Serializable]
public class BodyPartState
{
    [ToggleLeft]
    [OnValueChanged("OnStateChanged")]
    public bool IsActive = true;

    [ReadOnly, HideLabel]
    public string Name;

    [HideInInspector] public CharacterModularApplier Applier;
    [HideInInspector] public PartVariation Variation;

    private void OnStateChanged() => Applier?.ApplyCurrentBodySet();
}


public class CharacterModularApplier : MonoBehaviour
{
    [Title("Data Source")]
    [Required]
    public CharacterModularData ModularData;

    [Title("Current Configuration")]
    [OnValueChanged("OnBodySetChanged")]
    [ValueDropdown("GetBodySetNames")]
    [InfoBox("Chọn bộ cơ thể cơ bản cho nhân vật.")]
    public string SelectedBodySet;

    [ShowIf("SelectedBodySet")]
    [ListDrawerSettings(IsReadOnly = true, ShowIndexLabels = false)]
    public List<BodyPartState> CurrentBodyParts = new List<BodyPartState>();

    [DictionaryDrawerSettings(KeyLabel = "Group", ValueLabel = "Selection")]
    [OnInspectorGUI("DrawRefreshButton")]
    public Dictionary<string, PartSelection> CurrentPartSelections = new Dictionary<string, PartSelection>();

    private Transform _skeletonRootInstance;
    private readonly Dictionary<string, GameObject> _currentBodyPartInstances = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _currentlyEquippedParts = new Dictionary<string, GameObject>();

    private IEnumerable<string> GetBodySetNames() => ModularData?.BodySets.Select(b => b.Name) ?? Enumerable.Empty<string>();

    private void DrawRefreshButton()
    {
        if (GUILayout.Button("Refresh Part Selections From Data"))
        {
            InitializePartSelections();
        }
    }

    [Button(ButtonSizes.Large), PropertyOrder(-1)]
    public void BuildCharacter()
    {
        if (ModularData == null) return;

        ClearCharacter();
        InstantiateSkeleton();
        OnBodySetChanged(); // This will handle populating and applying the body
        InitializePartSelections();
        ApplyAllPartSelections();
    }

    private void OnBodySetChanged()
    {
        PopulateBodyPartStates();
        ApplyCurrentBodySet();
    }

    private void PopulateBodyPartStates()
    {
        CurrentBodyParts.Clear();
        BodySet bodySet = ModularData?.BodySets.FirstOrDefault(b => b.Name == SelectedBodySet);
        if (bodySet == null) return;

        foreach (var partVariation in bodySet.BodyParts)
        {
            CurrentBodyParts.Add(new BodyPartState
            {
                Name = partVariation.Name,
                Variation = partVariation,
                Applier = this,
                IsActive = true
            });
        }
    }

    private void ClearCharacter()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        _skeletonRootInstance = null;
        _currentBodyPartInstances.Clear();
        _currentlyEquippedParts.Clear();
    }

    private void InstantiateSkeleton()
    {
        if (ModularData.SkeletonRoot != null)
        {
            _skeletonRootInstance = Instantiate(ModularData.SkeletonRoot, transform);
            _skeletonRootInstance.name = "Skeleton";
        }
    }

    public void ApplyCurrentBodySet()
    {
        // Destroy parts that are no longer in the list or are disabled
        var keysToRemove = new List<string>();
        foreach (var pair in _currentBodyPartInstances)
        {
            var state = CurrentBodyParts.FirstOrDefault(p => p.Variation.Name == pair.Key);
            if (state == null || !state.IsActive)
            {
                if (pair.Value != null) DestroyImmediate(pair.Value);
                keysToRemove.Add(pair.Key);
            }
        }
        foreach (var key in keysToRemove) _currentBodyPartInstances.Remove(key);

        // Instantiate new or re-enabled parts
        foreach (var partState in CurrentBodyParts)
        {
            if (partState.IsActive && !_currentBodyPartInstances.ContainsKey(partState.Variation.Name))
            {
                if (partState.Variation.Prefab == null) continue;
                GameObject partInstance = Instantiate(partState.Variation.Prefab, transform);
                partInstance.name = partState.Variation.Name;
                RebindSkinnedMeshRenderer(partInstance.GetComponent<SkinnedMeshRenderer>());
                _currentBodyPartInstances[partState.Variation.Name] = partInstance;
            }
        }
    }

    private void InitializePartSelections()
    {
        if (ModularData == null) return;

        var existingSelection = new Dictionary<string, PartSelection>(CurrentPartSelections);
        CurrentPartSelections.Clear();
        foreach (var category in ModularData.Categories)
        {
            foreach (var group in category.Groups)
            {
                if (!CurrentPartSelections.ContainsKey(group.Name))
                {
                    CurrentPartSelections.Add(group.Name, new PartSelection
                    {
                        GroupName = group.Name,
                        Applier = this,
                        SelectedVariation = existingSelection.ContainsKey(group.Name) ? existingSelection[group.Name].SelectedVariation : null
                    });
                }
            }
        }
    }

    private void ApplyAllPartSelections()
    {
        foreach (var selection in CurrentPartSelections)
        {
            EquipPart(selection.Key, selection.Value.SelectedVariation);
        }
    }

    public void EquipPart(string groupName, string variationName)
    {
        UnequipPart(groupName);

        if (string.IsNullOrEmpty(variationName)) return;

        GameObject partPrefab = GetPrefabForVariation(groupName, variationName);
        if (partPrefab == null) return;

        GameObject newPartInstance = Instantiate(partPrefab, transform);
        newPartInstance.name = variationName;

        RebindSkinnedMeshRenderer(newPartInstance.GetComponent<SkinnedMeshRenderer>());

        _currentlyEquippedParts[groupName] = newPartInstance;
    }

    private void UnequipPart(string groupName)
    {
        if (_currentlyEquippedParts.TryGetValue(groupName, out GameObject equippedPart))
        {
            if (equippedPart != null)
            {
                if (Application.isPlaying) Destroy(equippedPart);
                else DestroyImmediate(equippedPart);
            }
            _currentlyEquippedParts.Remove(groupName);
        }
    }

    private void RebindSkinnedMeshRenderer(SkinnedMeshRenderer smr)
    {
        if (_skeletonRootInstance == null || smr == null) return;

        var boneMap = _skeletonRootInstance.GetComponentsInChildren<Transform>().ToDictionary(b => b.name, b => b);
        var newBones = new Transform[smr.bones.Length];

        for (int i = 0; i < smr.bones.Length; i++)
        {
            if (smr.bones[i] != null && boneMap.TryGetValue(smr.bones[i].name, out Transform newBone))
            {
                newBones[i] = newBone;
            }
            else
            {
                newBones[i] = smr.bones[i];
            }
        }

        smr.bones = newBones;
        if (smr.rootBone != null && boneMap.ContainsKey(smr.rootBone.name))
        {
            smr.rootBone = boneMap[smr.rootBone.name];
        }
    }

    internal IEnumerable<string> GetVariationsForGroup(string groupName)
    {
        if (ModularData == null) yield break;

        yield return null; // Option for "None"

        var variations = ModularData.Categories
            .SelectMany(cat => cat.Groups)
            .FirstOrDefault(grp => grp.Name == groupName)?
            .Variations;

        if (variations != null)
        {
            foreach (var variation in variations)
            {
                yield return variation.Name;
            }
        }
    }

    private GameObject GetPrefabForVariation(string groupName, string variationName)
    {
        if (ModularData == null) return null;

        return ModularData.Categories
            .SelectMany(cat => cat.Groups)
            .FirstOrDefault(grp => grp.Name == groupName)?
            .Variations
            .FirstOrDefault(var => var.Name == variationName)?.Prefab;
    }
}