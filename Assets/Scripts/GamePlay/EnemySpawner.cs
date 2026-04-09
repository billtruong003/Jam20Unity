using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EchoMage.Enemies;
using UnityEngine;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace EchoMage.Spawning
{
    public class EnemySpawner : MonoBehaviour
    {
        public event Action<int> OnEndlessCycleStarted;
        public event Action OnBossSpawned;
        public event Action OnBossKilled;

        [Header("Wave Configuration")]
        [SerializeField] private List<WaveData> _waves;
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Spawn Limits")]
        [Tooltip("Số quái tối đa tồn tại cùng lúc trên map.")]
        [SerializeField] private int _maxActiveEnemies = 30;

        [Header("Endless Mode Scaling (fallback nếu không có DifficultyManager)")]
        [SerializeField] private float _statMultiplierPerCycle = 1.2f;

        [Header("Boss Configuration")]
        [Tooltip("Danh sách boss prefabs — mỗi lần spawn sẽ random 1 con từ list này.")]
        [SerializeField] private GameObject[] _bossPrefabs;
        [Tooltip("Boss đầu tiên spawn sau bao nhiêu giây từ khi game bắt đầu.")]
        [SerializeField] private float _bossFirstSpawnTime = 600f;
        [Tooltip("Cứ mỗi bao nhiêu giây sẽ spawn thêm 1 boss (kể cả khi boss cũ còn sống).")]
        [SerializeField] private float _bossSpawnInterval = 30f;
        [SerializeField] private Transform _bossSpawnPoint;
        [SerializeField] private int _bossKillScore = 500;
        [SerializeField] private float _bossDeathExplosionRadius = 25f;

        private int _currentWaveIndex = 0;
        private int _endlessCycleCount = 1;
        private Coroutine _spawnCoroutine;
        private Coroutine _bossTimerCoroutine;

        // Boss mới nhất (cho UI HP bar). Nhiều boss có thể tồn tại cùng lúc.
        private GameObject _latestBossInstance;
        private float _gameTimer = 0f;
        private bool _isSpawning = false;
        private int _activeEnemyCount = 0;

        private void Start()
        {
            if (!AreSpawnPointsValid())
            {
                this.enabled = false;
                return;
            }
            StartCoroutine(InitialSpawnRoutine());
        }

        private void Update()
        {
            if (!_isSpawning) return;
            _gameTimer += Time.deltaTime;
        }

        private IEnumerator InitialSpawnRoutine()
        {
            yield return null;
            if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            {
                ResetAndRestartWaves();
            }
        }

        public void ResetAndRestartWaves()
        {
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            if (_bossTimerCoroutine != null) StopCoroutine(_bossTimerCoroutine);

            _currentWaveIndex = 0;
            _endlessCycleCount = 1;
            _gameTimer = 0f;
            _activeEnemyCount = 0;
            _latestBossInstance = null;
            _isSpawning = true;

            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.StartDifficulty();
            }

            StartNextWave();

            if (_bossPrefabs != null && _bossPrefabs.Length > 0)
            {
                _bossTimerCoroutine = StartCoroutine(BossSpawnTimerRoutine());
            }
        }

        private void StartNextWave()
        {
            if (_currentWaveIndex >= _waves.Count)
            {
                _endlessCycleCount++;
                _currentWaveIndex = _waves.Count - 1;
                OnEndlessCycleStarted?.Invoke(_endlessCycleCount);
            }

            _spawnCoroutine = StartCoroutine(SpawnWave(_waves[_currentWaveIndex]));
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            foreach (var entry in wave.WaveEntries)
            {
                // [FIX] Lấy spawn count từ DifficultyManager curve thay vì flat multiplier
                float countMultiplier = DifficultyManager.Instance != null
                    ? DifficultyManager.Instance.SpawnCountMultiplier
                    : 0.7f; // fallback

                int adjustedCount = Mathf.Max(1, Mathf.RoundToInt(entry.Count * countMultiplier));

                // [FIX] Lấy spawn rate từ DifficultyManager curve
                float rateMultiplier = DifficultyManager.Instance != null
                    ? DifficultyManager.Instance.SpawnRateMultiplier
                    : 1f;

                float adjustedInterval = entry.SpawnInterval * rateMultiplier;
                adjustedInterval = Mathf.Max(0.15f, adjustedInterval); // Không nhanh hơn 0.15s

                for (int i = 0; i < adjustedCount; i++)
                {
                    while (_activeEnemyCount >= _maxActiveEnemies)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }

                    SpawnEnemy(entry.EnemyPrefab);
                    yield return new WaitForSeconds(adjustedInterval);
                }
            }

            // [FIX] TimeToNextWave cũng scale theo spawn rate
            float nextWaveDelay = wave.TimeToNextWave;
            if (DifficultyManager.Instance != null)
            {
                nextWaveDelay *= DifficultyManager.Instance.SpawnRateMultiplier;
                nextWaveDelay = Mathf.Max(1f, nextWaveDelay);
            }

            yield return new WaitForSeconds(nextWaveDelay);

            _currentWaveIndex++;
            StartNextWave();
        }

        private void SpawnEnemy(GameObject enemyPrefab)
        {
            Transform spawnPoint = _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
            GameObject enemyInstance = ObjectPoolManager.Instance.Spawn(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            if (enemyInstance.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                // [FIX] Truyền DifficultyManager multipliers thay vì flat cycle multiplier
                float healthMult, damageMult, speedMult;

                if (DifficultyManager.Instance != null)
                {
                    healthMult = DifficultyManager.Instance.EnemyHealthMultiplier;
                    damageMult = DifficultyManager.Instance.EnemyDamageMultiplier;
                    speedMult = DifficultyManager.Instance.EnemySpeedMultiplier;
                }
                else
                {
                    // Fallback: dùng cycle multiplier cũ
                    float cycleMult = Mathf.Pow(_statMultiplierPerCycle, _endlessCycleCount - 1);
                    healthMult = cycleMult;
                    damageMult = cycleMult;
                    speedMult = 1f;
                }

                enemyBase.InitializeWithDifficulty(
                    GameManager.Instance.PlayerTransform,
                    healthMult,
                    damageMult,
                    speedMult
                );
            }

            _activeEnemyCount++;
        }

        public void NotifyEnemyRemoved()
        {
            _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
        }

        #region Boss System

        /// <summary>
        /// Boss spawn theo interval liên tục:
        /// 1. Đợi _bossFirstSpawnTime cho boss đầu tiên
        /// 2. Spawn boss
        /// 3. Đợi _bossSpawnInterval
        /// 4. Spawn boss tiếp (dù con cũ còn sống) → quay lại bước 3
        /// 
        /// Nhiều boss tồn tại cùng lúc. Mỗi con tự quản lý death riêng.
        /// Khi player chết → coroutine bị stop bởi ResetAndRestartWaves.
        /// Khi respawn → coroutine mới chạy, đếm lại từ đầu.
        /// </summary>
        private IEnumerator BossSpawnTimerRoutine()
        {
            // Đợi boss đầu tiên
            while (_gameTimer < _bossFirstSpawnTime)
            {
                yield return new WaitForSeconds(1f);
            }

            // Loop vô hạn: spawn → đợi interval → spawn tiếp
            while (true)
            {
                try
                {
                    SpawnBoss();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EnemySpawner] SpawnBoss failed: {e.Message}\n{e.StackTrace}");
                }

                // Đợi interval rồi spawn con tiếp — KHÔNG đợi con cũ chết
                yield return new WaitForSeconds(_bossSpawnInterval);
            }
        }

        private void SpawnBoss()
        {
            if (_bossPrefabs == null || _bossPrefabs.Length == 0) return;
            if (GameManager.Instance == null || GameManager.Instance.PlayerTransform == null) return;

            GameObject chosenBossPrefab = _bossPrefabs[UnityEngine.Random.Range(0, _bossPrefabs.Length)];

            Transform spawnPoint = _bossSpawnPoint != null
                ? _bossSpawnPoint
                : _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];

            GameObject bossInstance = ObjectPoolManager.Instance.Spawn(chosenBossPrefab, spawnPoint.position, spawnPoint.rotation);

            if (bossInstance != null && bossInstance.TryGetComponent<BossEnemy>(out var boss))
            {
                float healthMult = DifficultyManager.Instance != null
                    ? DifficultyManager.Instance.EnemyHealthMultiplier
                    : Mathf.Pow(_statMultiplierPerCycle, _endlessCycleCount - 1);

                boss.Initialize(GameManager.Instance.PlayerTransform, healthMult);
                boss.OnBossDeath += HandleBossDeath;
            }

            // Track con mới nhất cho UI HP bar
            _latestBossInstance = bossInstance;

            try
            {
                OnBossSpawned?.Invoke();

                if (bossInstance != null && bossInstance.TryGetComponent<BossEnemy>(out var bossForHP))
                {
                    bossForHP.BroadcastHealth();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnemySpawner] Boss spawned nhưng event/UI lỗi: {e.Message}");
            }
        }

        private void HandleBossDeath(BossEnemy boss)
        {
            boss.OnBossDeath -= HandleBossDeath;

            try
            {
                GameSessionManager.Instance.AddScore(_bossKillScore);
                KillNearbyEnemies(boss.transform.position, _bossDeathExplosionRadius);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnemySpawner] Lỗi trong HandleBossDeath: {e.Message}");
            }

            OnBossKilled?.Invoke();
        }

        private void KillNearbyEnemies(Vector3 center, float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);
            int explosionKillScore = 5;

            foreach (var col in colliders)
            {
                if (col.TryGetComponent<EnemyBase>(out var enemy))
                {
                    try
                    {
                        ObjectPoolManager.Instance.Spawn("EnemyExplosionFX", col.transform.position, Quaternion.identity);
                    }
                    catch { }

                    enemy.ForceKill();
                    GameSessionManager.Instance.AddScore(explosionKillScore);
                }
            }
        }

        #endregion

        private bool AreSpawnPointsValid()
        {
            if (_spawnPoints.Length > 0) return true;
            Debug.LogError("No spawn points assigned to the spawner.", this);
            return false;
        }

        public bool IsBossActive => _latestBossInstance != null && _latestBossInstance.activeInHierarchy;
        public GameObject CurrentBossInstance => _latestBossInstance;
    }
}