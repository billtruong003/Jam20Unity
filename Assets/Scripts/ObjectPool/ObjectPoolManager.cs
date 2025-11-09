using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public interface IPoolableObject
{
    void OnObjectSpawn();
    void OnObjectReturn();
}

public class ObjectPoolManager : SerializedMonoBehaviour
{
    #region Singleton
    public static ObjectPoolManager Instance { get; private set; }
    #endregion

    #region Inspector Configuration
    [Title("Pool Configuration")]
    [Tooltip("Danh sách tất cả các đối tượng có thể được pool trong game. Hãy định nghĩa chúng ở đây.")]
    [ListDrawerSettings(AddCopiesLastElement = true)]
    [SerializeField]
    private List<PoolableItem> _initialPools = new List<PoolableItem>();
    #endregion

    #region Private Fields
    private readonly Dictionary<string, PoolableItem> _registry = new Dictionary<string, PoolableItem>();
    private readonly Dictionary<int, Queue<GameObject>> _poolDictionary = new Dictionary<int, Queue<GameObject>>();
    private readonly Dictionary<int, int> _prefabInstanceIdMap = new Dictionary<int, int>();
    private readonly Dictionary<int, Transform> _poolParents = new Dictionary<int, Transform>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        InitializePools();
    }
    #endregion

    #region Public API
    /// <summary>
    /// Lấy một đối tượng từ pool bằng ID đã được định nghĩa sẵn.
    /// </summary>
    public GameObject Spawn(string id, Vector3 position, Quaternion rotation)
    {
        if (!_registry.TryGetValue(id, out PoolableItem item))
        {
            Debug.LogError($"[ObjectPoolManager] Không tìm thấy pool với ID: '{id}'. Hãy chắc chắn rằng nó đã được định nghĩa trong Inspector.");
            return null;
        }

        return Spawn(item.Prefab, position, rotation);
    }

    /// <summary>
    /// Lấy một đối tượng từ pool bằng cách truyền trực tiếp Prefab.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPoolManager] Prefab không thể là null.");
            return null;
        }

        int prefabId = prefab.GetInstanceID();
        Queue<GameObject> objectQueue = GetOrCreateObjectQueue(prefabId);

        GameObject objectToSpawn = GetObjectFromQueueOrCreate(objectQueue, prefab);

        SetupSpawnedObject(objectToSpawn, position, rotation);
        return objectToSpawn;
    }

    /// <summary>
    /// Trả một đối tượng về lại pool để tái sử dụng.
    /// </summary>
    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null) return;

        objectToReturn.GetComponent<IPoolableObject>()?.OnObjectReturn();

        int instanceId = objectToReturn.GetInstanceID();
        if (_prefabInstanceIdMap.TryGetValue(instanceId, out int prefabId))
        {
            _poolDictionary[prefabId].Enqueue(objectToReturn);
            Transform parent = GetOrCreatePoolParent(prefabId, objectToReturn.name.Replace("(Clone)", ""));
            objectToReturn.transform.SetParent(parent);
            objectToReturn.SetActive(false);
        }
        else
        {
            // Nếu đối tượng không được quản lý bởi pool, hủy nó đi.
            Debug.LogWarning($"[ObjectPoolManager] Đối tượng '{objectToReturn.name}' không thuộc pool. Sẽ bị hủy thay vì trả về.");
            Destroy(objectToReturn);
        }
    }
    #endregion

    #region Private Initialization
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void InitializePools()
    {
        foreach (var item in _initialPools)
        {
            if (item.Prefab == null || string.IsNullOrEmpty(item.Id))
            {
                Debug.LogWarning($"[ObjectPoolManager] Bỏ qua một mục trong pool vì ID hoặc Prefab rỗng.");
                continue;
            }

            if (_registry.ContainsKey(item.Id))
            {
                Debug.LogWarning($"[ObjectPoolManager] ID '{item.Id}' bị trùng lặp. Sẽ bỏ qua mục này.");
                continue;
            }

            _registry[item.Id] = item;
            PreloadPool(item.Prefab, item.PreloadCount);
        }
    }

    private void PreloadPool(GameObject prefab, int count)
    {
        if (count <= 0) return;

        int prefabId = prefab.GetInstanceID();
        Queue<GameObject> objectQueue = GetOrCreateObjectQueue(prefabId);
        Transform parent = GetOrCreatePoolParent(prefabId, prefab.name);

        for (int i = 0; i < count; i++)
        {
            GameObject preloadedObject = Instantiate(prefab, parent);
            _prefabInstanceIdMap[preloadedObject.GetInstanceID()] = prefabId;
            preloadedObject.GetComponent<IPoolableObject>()?.OnObjectReturn();
            preloadedObject.SetActive(false);
            objectQueue.Enqueue(preloadedObject);
        }
    }
    #endregion

    #region Private Core Logic
    private Queue<GameObject> GetOrCreateObjectQueue(int prefabId)
    {
        if (!_poolDictionary.TryGetValue(prefabId, out Queue<GameObject> objectQueue))
        {   
            objectQueue = new Queue<GameObject>();
            _poolDictionary[prefabId] = objectQueue;
        }
        return objectQueue;
    }

    private GameObject GetObjectFromQueueOrCreate(Queue<GameObject> queue, GameObject prefab)
    {
        if (queue.Count > 0)
        {
            return queue.Dequeue();
        }

        GameObject newObject = Instantiate(prefab);
        _prefabInstanceIdMap[newObject.GetInstanceID()] = prefab.GetInstanceID();
        return newObject;
    }

    private void SetupSpawnedObject(GameObject obj, Vector3 position, Quaternion rotation)
    {
        obj.transform.SetParent(null);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        obj.GetComponent<IPoolableObject>()?.OnObjectSpawn();
    }

    private Transform GetOrCreatePoolParent(int prefabId, string prefabName)
    {
        if (!_poolParents.TryGetValue(prefabId, out Transform parent))
        {
            var parentObject = new GameObject($"{prefabName}_Pool");
            parent = parentObject.transform;
            parent.SetParent(this.transform);
            _poolParents[prefabId] = parent;
        }
        return parent;
    }
    #endregion
}