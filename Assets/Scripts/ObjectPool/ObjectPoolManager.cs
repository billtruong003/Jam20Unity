using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
using System.Linq;

namespace BillUtils.ObjectPooler
{
    public interface IPoolableObject
    {
        void OnObjectSpawn();
        void OnObjectReturn();
    }

    [Serializable]
    public class PoolableItem
    {
        [HorizontalGroup("ID", Width = 100), LabelWidth(30)]
        public string Id;

        [HorizontalGroup("Prefab")]
        public GameObject Prefab;

        [HorizontalGroup("Count"), LabelWidth(50)]
        [Min(0)] public int PreloadCount = 5;

        [Tooltip("Tự động mở rộng khi hết (nếu false thì trả về null)")]
        public bool AllowGrowth = true;

        [Tooltip("Tối đa số lượng object trong pool (0 = không giới hạn)")]
        [Min(0)] public int MaxSize = 0;

        public override string ToString() => $"{Id} ({Prefab?.name ?? "null"})";
    }

    public class ObjectPoolManager : SerializedMonoBehaviour
    {
        #region Singleton

        public static ObjectPoolManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Title("Pool Configuration", TitleAlignment = TitleAlignments.Centered)]
        [InfoBox("Định nghĩa các pool ở đây. Mỗi ID là duy nhất.", InfoMessageType.Info)]
        [ListDrawerSettings(
            ShowIndexLabels = true,
            DraggableItems = true,
            AddCopiesLastElement = true,
            HideRemoveButton = false,
            CustomAddFunction = nameof(AddNewPoolItem)
        )]
        [SerializeField] private List<PoolableItem> _initialPools = new();

        [Title("Runtime Debug")]
        [ReadOnly, ShowInInspector, PropertyOrder(100)]
        private int TotalPooledObjects => _poolDictionary.Values.Sum(q => q.Count);

        [ReadOnly, ShowInInspector, PropertyOrder(101)]
        private int ActiveObjects => _activeCount;

        #endregion

        #region Private Fields

        private readonly Dictionary<string, PoolableItem> _registry = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, Queue<GameObject>> _poolDictionary = new();
        private readonly Dictionary<int, int> _prefabToPoolIdMap = new();
        private readonly Dictionary<int, Transform> _poolParents = new();
        private readonly HashSet<int> _activeInstances = new();
        private int _activeCount = 0;

        private const string POOL_PARENT_NAME = "Pooled Objects";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSingleton();
            InitializePoolHierarchy();
            RegisterAndPreloadPools();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Initialization

        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {

                Destroy(gameObject);
                return;
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

        }

        private void InitializePoolHierarchy()
        {
            Transform root = transform.Find(POOL_PARENT_NAME);
            if (root == null)
            {
                root = new GameObject(POOL_PARENT_NAME).transform;
                root.SetParent(transform);
                root.localPosition = Vector3.zero;
            }
        }

        private void RegisterAndPreloadPools()
        {
            _registry.Clear();
            _poolDictionary.Clear();
            _prefabToPoolIdMap.Clear();
            _poolParents.Clear();
            _activeInstances.Clear();
            _activeCount = 0;

            foreach (var item in _initialPools)
            {
                if (!ValidatePoolItem(item, out string error))
                {
                    Debug.LogError($"[ObjectPoolManager] {error}");
                    continue;
                }

                if (_registry.ContainsKey(item.Id))
                {
                    Debug.LogError($"[ObjectPoolManager] ID trùng lặp: '{item.Id}'. Bỏ qua.");
                    continue;
                }

                _registry[item.Id] = item;
                int prefabId = item.Prefab.GetInstanceID();
                _prefabToPoolIdMap[prefabId] = prefabId;

                PreloadPool(item);
            }

            Debug.Log($"[ObjectPoolManager] Đã khởi tạo {_registry.Count} pool.");
        }

        private bool ValidatePoolItem(PoolableItem item, out string error)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                error = "Pool ID không được để trống.";
                return false;
            }
            if (item.Prefab == null)
            {
                error = $"Prefab cho ID '{item.Id}' là null.";
                return false;
            }
            error = null;
            return true;
        }

        private void PreloadPool(PoolableItem item)
        {
            int prefabId = item.Prefab.GetInstanceID();
            var queue = GetOrCreateQueue(prefabId);
            var parent = GetOrCreatePoolParent(prefabId, GetCleanName(item.Prefab.name));

            int toPreload = Mathf.Max(0, item.PreloadCount);
            for (int i = 0; i < toPreload; i++)
            {
                if (item.MaxSize > 0 && queue.Count >= item.MaxSize) break;

                GameObject obj = CreatePooledObject(item.Prefab, parent);
                queue.Enqueue(obj);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawn bằng ID
        /// </summary>
        public GameObject Spawn(string id, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("[ObjectPoolManager] ID không hợp lệ.");
                return null;
            }

            if (!_registry.TryGetValue(id, out PoolableItem item))
            {
                Debug.LogError($"[ObjectPoolManager] Không tìm thấy pool ID: '{id}'");
                return null;
            }

            return SpawnInternal(item, position, rotation, parent, worldPositionStays);
        }

        /// <summary>
        /// Spawn bằng Prefab
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true)
        {
            if (prefab == null)
            {
                Debug.LogError("[ObjectPoolManager] Prefab null.");
                return null;
            }

            int prefabId = prefab.GetInstanceID();
            if (!_prefabToPoolIdMap.ContainsKey(prefabId))
            {
                // Tự động tạo pool tạm nếu chưa đăng ký
                var tempItem = new PoolableItem
                {
                    Id = prefab.name + "_Auto",
                    Prefab = prefab,
                    PreloadCount = 0,
                    AllowGrowth = true
                };
                _registry[tempItem.Id] = tempItem;
                _prefabToPoolIdMap[prefabId] = prefabId;
            }

            if (!_registry.TryGetValue(GetPoolIdFromPrefab(prefab), out PoolableItem item))
                return null;

            return SpawnInternal(item, position, rotation, parent, worldPositionStays);
        }

        /// <summary>
        /// Trả object về pool
        /// </summary>
        public bool Despawn(GameObject obj)
        {
            if (obj == null) return false;

            int instanceId = obj.GetInstanceID();
            if (!_activeInstances.Remove(instanceId))
            {
                Debug.LogWarning($"[ObjectPoolManager] Object '{obj.name}' không được spawn từ pool.");
                Destroy(obj);
                return false;
            }

            _activeCount--;

            obj.GetComponent<IPoolableObject>()?.OnObjectReturn();

            if (_prefabToPoolIdMap.TryGetValue(obj.GetInstanceID(), out int prefabId) ||
                TryGetPrefabIdFromInstance(obj, out prefabId))
            {
                var queue = GetOrCreateQueue(prefabId);
                var item = GetPoolItemFromPrefabId(prefabId);

                if (item != null && item.MaxSize > 0 && queue.Count >= item.MaxSize)
                {
                    Destroy(obj);
                    return true;
                }

                obj.SetActive(false);
                obj.transform.SetParent(GetOrCreatePoolParent(prefabId, GetCleanName(obj.name)), false);
                queue.Enqueue(obj);
                return true;
            }
            else
            {
                Destroy(obj);
                return false;
            }
        }

        /// <summary>
        /// Dọn sạch toàn bộ pool
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var queue in _poolDictionary.Values)
            {
                while (queue.Count > 0)
                {
                    var obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            _poolDictionary.Clear();
            _activeInstances.Clear();
            _activeCount = 0;

            foreach (Transform child in transform.Find(POOL_PARENT_NAME))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        #endregion

        #region Core Logic

        private GameObject SpawnInternal(PoolableItem item, Vector3 pos, Quaternion rot, Transform parent, bool worldPosStays)
        {
            int prefabId = item.Prefab.GetInstanceID();
            var queue = GetOrCreateQueue(prefabId);

            GameObject obj;

            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else if (item.AllowGrowth && (item.MaxSize == 0 || _poolDictionary[prefabId].Count < item.MaxSize))
            {
                obj = CreatePooledObject(item.Prefab, GetOrCreatePoolParent(prefabId, GetCleanName(item.Prefab.name)));
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{item.Id}' đã đầy hoặc không cho phép mở rộng.");
                return null;
            }

            // Setup
            obj.transform.SetParent(parent, worldPosStays);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);

            int instanceId = obj.GetInstanceID();
            _activeInstances.Add(instanceId);
            _activeCount++;

            obj.GetComponent<IPoolableObject>()?.OnObjectSpawn();

            return obj;
        }

        private GameObject CreatePooledObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.name = GetCleanName(prefab.name);
            int instanceId = obj.GetInstanceID();
            int prefabId = prefab.GetInstanceID();

            // Ghi nhận ánh xạ
            if (!_prefabToPoolIdMap.ContainsKey(instanceId))
                _prefabToPoolIdMap[instanceId] = prefabId;

            return obj;
        }

        private Queue<GameObject> GetOrCreateQueue(int prefabId)
        {
            if (!_poolDictionary.TryGetValue(prefabId, out var queue))
            {
                queue = new Queue<GameObject>();
                _poolDictionary[prefabId] = queue;
            }
            return queue;
        }

        private Transform GetOrCreatePoolParent(int prefabId, string name)
        {
            if (!_poolParents.TryGetValue(prefabId, out Transform parent))
            {
                var parentObj = new GameObject($"{name}_Pool");
                parent = parentObj.transform;
                parent.SetParent(transform.Find(POOL_PARENT_NAME));
                _poolParents[prefabId] = parent;
            }
            return parent;
        }

        private string GetCleanName(string name) => name.Replace("(Clone)", "").Trim();

        private string GetPoolIdFromPrefab(GameObject prefab)
        {
            foreach (var kvp in _registry)
                if (kvp.Value.Prefab == prefab) return kvp.Key;
            return null;
        }

        private PoolableItem GetPoolItemFromPrefabId(int prefabId)
        {
            foreach (var kvp in _registry)
                if (kvp.Value.Prefab.GetInstanceID() == prefabId) return kvp.Value;
            return null;
        }

        private bool TryGetPrefabIdFromInstance(GameObject instance, out int prefabId)
        {
            prefabId = 0;
            foreach (var kvp in _prefabToPoolIdMap)
            {
                if (kvp.Key == instance.GetInstanceID())
                {
                    prefabId = kvp.Value;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private PoolableItem AddNewPoolItem()
        {
            return new PoolableItem { Id = "New_Pool_" + _initialPools.Count };
        }

        [Button("Clear All Pools", ButtonSizes.Medium), PropertyOrder(102)]
        private void Editor_ClearAll() => ClearAllPools();

        [Button("Refresh Pools", ButtonSizes.Medium), PropertyOrder(103)]
        private void Editor_Refresh() => RegisterAndPreloadPools();
#endif

        #endregion
    }
}