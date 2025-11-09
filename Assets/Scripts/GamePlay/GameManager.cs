using EchoMage.Player;
using EchoMage.UI;
using EchoMage.World;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using EchoMage.Spawning;
using BillUtils.ObjectPooler;

namespace EchoMage.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameObject> OnPlayerSpawned;

        [Header("Player Ref")]
        public AfterImageController AfterImageController;

        [Header("Core References")]
        public PlayerSpawner PlayerSpawner;
        public UIManager UIManager;
        public DespairSystem DespairSystem;
        public EnemySpawner EnemySpawner;

        [Header("Gameplay State")]
        public float WorldThreatLevel = 1f;
        private bool _isGameOver = false;

        [Header("Echo System")]
        [SerializeField] private GameObject echoGravePrefab;

        private readonly HashSet<GameObject> _activeEnemies = new HashSet<GameObject>();
        private readonly List<EchoGrave> _activeGraves = new List<EchoGrave>();

        public PlayerStats PlayerStats { get; private set; }
        public Transform PlayerTransform { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (_isGameOver || PlayerTransform == null) return;

            DespairSystem.UpdateDespair(_activeEnemies.Count, Time.deltaTime);
        }

        public void NotifyPlayerSpawned(GameObject playerInstance)
        {
            _isGameOver = false;
            PlayerTransform = playerInstance.transform;
            PlayerStats = playerInstance.GetComponent<PlayerStats>();
            ResetAllGravesForNewLife();
            OnPlayerSpawned?.Invoke(playerInstance);
        }

        public void HandlePlayerDeath(PlayerStats deadPlayerStats, Vector3 deathPosition)
        {
            PlayerTransform = null;
            CreateEchoGrave(deadPlayerStats, deathPosition);

            Time.timeScale = 0f;
            CleanupAllEnemies();
            UIManager.ShowContinueScreen();
        }

        public void ContinueFromDeath()
        {
            Time.timeScale = 1f;
            UIManager.HideContinueScreen();
            EnemySpawner.ResetAndRestartWaves();
            PlayerSpawner.RequestRespawn();
        }

        private void CreateEchoGrave(PlayerStats stats, Vector3 position)
        {
            if (echoGravePrefab == null) return;

            GameObject graveInstance = Instantiate(echoGravePrefab, position, Quaternion.identity);
            if (graveInstance.TryGetComponent<EchoGrave>(out var echoGrave))
            {
                var echoData = new Echoes.PlayerEchoData(stats, position);
                echoGrave.Initialize(echoData);
            }
        }

        public void EndGame(string reason)
        {
            if (_isGameOver) return;

            _isGameOver = true;
            Time.timeScale = 0f;
            UIManager.ShowGameOverScreen(reason);
        }

        public void RegisterEnemy(GameObject enemy) => _activeEnemies.Add(enemy);

        public void UnregisterEnemy(GameObject enemy)
        {
            // === ĐÂY LÀ PHẦN SỬA LỖI ===
            // Phương thức Remove của HashSet trả về true nếu phần tử tồn tại và đã được xóa thành công.
            // Đây chính là điều kiện hoàn hảo để xác định một "kill" hợp lệ.
            if (_activeEnemies.Remove(enemy))
            {
                DespairSystem.ReduceDespairOnKill();
            }
        }

        private void CleanupAllEnemies()
        {
            foreach (var enemy in _activeEnemies.ToList())
            {
                ObjectPoolManager.Instance.Despawn(enemy);
            }
            _activeEnemies.Clear();
        }

        public void RegisterGrave(EchoGrave grave)
        {
            if (!_activeGraves.Contains(grave)) _activeGraves.Add(grave);
        }

        public void UnregisterGrave(EchoGrave grave)
        {
            _activeGraves.Remove(grave);
        }

        private void ResetAllGravesForNewLife()
        {
            foreach (var grave in _activeGraves.ToList())
            {
                grave.ResetForNewLife();
            }
        }
    }
}