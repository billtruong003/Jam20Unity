using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[System.Serializable]
public class PartVariation
{
    [ReadOnly]
    public string Name;

    [Required]
    [AssetsOnly]
    [PreviewField(ObjectFieldAlignment.Center, Height = 75)]
    public GameObject Prefab;
}

[System.Serializable]
public class PartGroup
{
    [ReadOnly]
    public string Name;

    [TableList(ShowIndexLabels = false)]
    public List<PartVariation> Variations = new List<PartVariation>();
}

[System.Serializable]
public class PartCategory
{
    [ReadOnly]
    public string Name;

    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<PartGroup> Groups = new List<PartGroup>();
}

[System.Serializable]
public class BodySet
{
    [ReadOnly]
    public string Name;

    [AssetsOnly]
    [TableList(ShowIndexLabels = false)]
    public List<PartVariation> BodyParts = new List<PartVariation>();
}

[CreateAssetMenu(fileName = "NewCharacterModularData", menuName = "Modular Character/Character Data")]
public class CharacterModularData : ScriptableObject
{
    [Title("Base Configuration", bold: true)]
    [Required("You must assign the skeleton root transform.")]
    [AssetsOnly]
    [InfoBox("Đây là root của hệ thống xương, thường là 'Hips' hoặc một bone gốc tương tự.")]
    public Transform SkeletonRoot;

    [Title("Body Sets", bold: true)]
    [ListDrawerSettings(ShowIndexLabels = true)]
    [InfoBox("Danh sách các bộ cơ thể. Mỗi bộ chứa các bộ phận như đầu, mình, tay, chân...")]
    public List<BodySet> BodySets = new List<BodySet>();

    [Title("Modular Parts Catalog", bold: true)]
    [ListDrawerSettings(ShowIndexLabels = true, AddCopiesLastElement = true)]
    public List<PartCategory> Categories = new List<PartCategory>();
}