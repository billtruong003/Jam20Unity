
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class PoolableItem
{
    [Tooltip("ID định danh duy nhất để gọi spawn đối tượng này từ bất kỳ đâu.")]
    public string Id;

    [Tooltip("Prefab của đối tượng sẽ được tạo ra.")]
    [AssetsOnly]
    public GameObject Prefab;

    [Tooltip("Số lượng đối tượng được tạo sẵn khi game bắt đầu để tránh giật lag.")]
    [MinValue(0)]
    public int PreloadCount = 0;
}