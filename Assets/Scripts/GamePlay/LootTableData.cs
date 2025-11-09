using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace EchoMage.Loot
{
    [System.Serializable]
    public struct LootDropItem
    {
        [AssetsOnly]
        public GameObject ItemPrefab;

        [Range(0f, 1f)]
        public float DropChance;
    }

    [CreateAssetMenu(fileName = "NewLootTable", menuName = "EchoMage/Loot Table")]
    public class LootTableData : ScriptableObject
    {
        [Tooltip("Danh sách các vật phẩm có thể rơi. Hệ thống sẽ duyệt từ trên xuống dưới và chỉ rơi ra vật phẩm ĐẦU TIÊN thỏa mãn tỉ lệ.")]
        public List<LootDropItem> PotentialDrops;
    }
}