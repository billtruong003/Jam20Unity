using EchoMage.Player;
using EchoMage.UI;
using EchoMage.World;
using EchoMage.AI;
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
        /// <summary>
        /// Event broadcast khi player chết — GhostCompanion và các hệ thống khác lắng nghe để tự cleanup.
        /// </summary>
        public event Action OnPlayerDied;

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

        [Header("Cleanup Tags")]
        [Tooltip("Các tag sẽ bị dọn khi player chết (loot drops, projectiles, v.v.)")]
        [SerializeField] private string[] cleanupTags = { "Pickup", "PlayerProjectile" };

        private readonly HashSet<GameObject> _activeEnemies = new HashSet<GameObject>();
        private readonly List<EchoGrave> _activeGraves = new List<EchoGrave>();
        // [MỚI] Track tất cả GhostCompanion đang sống
        private readonly List<GhostCompanion> _activeGhosts = new List<GhostCompanion>();

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

        private void Start()
        {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayGameplayMusic();
            }

            if (EnemySpawner != null)
            {
                EnemySpawner.OnBossSpawned += HandleBossSpawned;
                EnemySpawner.OnBossKilled += HandleBossKilled;
            }
        }

        private void OnDestroy()
        {
            if (EnemySpawner != null)
            {
                EnemySpawner.OnBossSpawned -= HandleBossSpawned;
                EnemySpawner.OnBossKilled -= HandleBossKilled;
            }
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

            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetGameOverState(false);
            }
        }

        public void HandlePlayerDeath(PlayerStats deadPlayerStats, Vector3 deathPosition)
        {
            PlayerTransform = null;
            CreateEchoGrave(deadPlayerStats, deathPosition);

            Time.timeScale = 0f;

            // [FIX] Broadcast event trước khi cleanup — GhostCompanion nghe để tự hủy
            OnPlayerDied?.Invoke();

            // [FIX] Dọn DẸP TOÀN BỘ: quái + ghost + loot + projectiles
            CleanupAllEnemies();
            CleanupAllGhosts();
            CleanupTaggedObjects();

            GameSessionManager.Instance.HandlePlayerDeath(DeathCause.HealthDepletion);
            UIManager.ShowContinueScreen();
        }

        public void ContinueFromDeath()
        {
            Time.timeScale = 1f;
            UIManager.HideContinueScreen();
            EnemySpawner.ResetAndRestartWaves();
            PlayerSpawner.RequestRespawn();

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayGameplayMusic();
            }
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

            OnPlayerDied?.Invoke();

            if (PauseManager.Instance != null)
                PauseManager.Instance.SetGameOverState(true);

            if (MusicManager.Instance != null)
                MusicManager.Instance.PlayGameOverMusic();

            GameSessionManager.Instance.HandlePlayerDeath(DeathCause.Despair);
            UIManager.ShowGameOverScreen(reason);
        }

        #region Enemy Tracking

        public void RegisterEnemy(GameObject enemy) => _activeEnemies.Add(enemy);

        public void UnregisterEnemy(GameObject enemy)
        {
            if (_activeEnemies.Remove(enemy))
            {
                DespairSystem.ReduceDespairOnKill();
                if (EnemySpawner != null)
                    EnemySpawner.NotifyEnemyRemoved();
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

        #endregion

        #region Ghost Companion Tracking

        /// <summary>
        /// [MỚI] GhostCompanion gọi khi được triệu hồi.
        /// </summary>
        public void RegisterGhost(GhostCompanion ghost)
        {
            if (!_activeGhosts.Contains(ghost))
                _activeGhosts.Add(ghost);
        }

        /// <summary>
        /// [MỚI] GhostCompanion gọi khi bị hủy.
        /// </summary>
        public void UnregisterGhost(GhostCompanion ghost)
        {
            _activeGhosts.Remove(ghost);
        }

        /// <summary>
        /// [MỚI] Hủy tất cả GhostCompanion khi player chết.
        /// </summary>
        private void CleanupAllGhosts()
        {
            foreach (var ghost in _activeGhosts.ToList())
            {
                if (ghost != null && ghost.gameObject != null)
                {
                    Destroy(ghost.gameObject);
                }
            }
            _activeGhosts.Clear();
        }

        #endregion

        #region Grave Tracking

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

        #endregion

        #region Loot / Projectile Cleanup

        /// <summary>
        /// [MỚI] Dọn tất cả object có tag trong cleanupTags (loot drops, projectiles bay dở).
        /// Dùng ObjectPool.Despawn nếu có IPoolableObject, nếu không thì Destroy.
        /// </summary>
        private void CleanupTaggedObjects()
        {
            foreach (string tag in cleanupTags)
            {
                GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
                foreach (var obj in objects)
                {
                    if (obj == null) continue;

                    // Thử despawn qua pool trước (hiệu quả hơn Destroy)
                    try
                    {
                        ObjectPoolManager.Instance.Despawn(obj);
                    }
                    catch
                    {
                        // Không thuộc pool → Destroy thường
                        Destroy(obj);
                    }
                }
            }
        }

        #endregion

        #region Boss Events

        private void HandleBossSpawned()
        {
            if (MusicManager.Instance != null)
                MusicManager.Instance.PlayBossMusic();

            UIManager.ShowBossHealthBar(EnemySpawner.CurrentBossInstance);
        }

        private void HandleBossKilled()
        {
            if (MusicManager.Instance != null)
                MusicManager.Instance.ResumeGameplayMusic();

            UIManager.HideBossHealthBar();
        }

        #endregion
    }
}