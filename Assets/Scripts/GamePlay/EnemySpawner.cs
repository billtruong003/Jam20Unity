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
        [SerializeField] private GameObject _bossPrefab;
        [SerializeField] private float _bossSpawnTime = 600f;
        [SerializeField] private Transform _bossSpawnPoint;
        [SerializeField] private int _bossKillScore = 500;
        [SerializeField] private float _bossDeathExplosionRadius = 25f;
        [SerializeField] private float _bossRespawnDelay = 120f;

        private int _currentWaveIndex = 0;
        private int _endlessCycleCount = 1;
        private Coroutine _spawnCoroutine;
        private Coroutine _bossTimerCoroutine;

        private GameObject _currentBossInstance;
        private bool _bossIsActive = false;
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
            _bossIsActive = false;
            _isSpawning = true;

            // [MỚI] Start difficulty timer nếu có DifficultyManager
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.StartDifficulty();
            }

            StartNextWave();

            if (_bossPrefab != null)
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

        private IEnumerator BossSpawnTimerRoutine()
        {
            while (_gameTimer < _bossSpawnTime)
            {
                yield return new WaitForSeconds(1f);
            }
            SpawnBoss();
        }

        private void SpawnBoss()
        {
            if (_bossPrefab == null || _bossIsActive) return;

            Transform spawnPoint = _bossSpawnPoint != null
                ? _bossSpawnPoint
                : _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];

            _currentBossInstance = ObjectPoolManager.Instance.Spawn(_bossPrefab, spawnPoint.position, spawnPoint.rotation);

            if (_currentBossInstance.TryGetComponent<BossEnemy>(out var boss))
            {
                float healthMult = DifficultyManager.Instance != null
                    ? DifficultyManager.Instance.EnemyHealthMultiplier
                    : Mathf.Pow(_statMultiplierPerCycle, _endlessCycleCount - 1);

                boss.Initialize(GameManager.Instance.PlayerTransform, healthMult);
                boss.OnBossDeath += HandleBossDeath;
            }

            _bossIsActive = true;
            OnBossSpawned?.Invoke();
        }

        private void HandleBossDeath(BossEnemy boss)
        {
            boss.OnBossDeath -= HandleBossDeath;
            _bossIsActive = false;

            GameSessionManager.Instance.AddScore(_bossKillScore);
            KillNearbyEnemies(boss.transform.position, _bossDeathExplosionRadius);
            OnBossKilled?.Invoke();

            if (_bossTimerCoroutine != null)
                StopCoroutine(_bossTimerCoroutine);

            _bossTimerCoroutine = StartCoroutine(RespawnBossAfterDelay());
        }

        private void KillNearbyEnemies(Vector3 center, float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);
            int explosionKillScore = 5;

            foreach (var col in colliders)
            {
                if (col.gameObject == _currentBossInstance) continue;
                if (col.TryGetComponent<EnemyBase>(out var enemy))
                {
                    ObjectPoolManager.Instance.Spawn("EnemyExplosionFX", col.transform.position, Quaternion.identity);
                    enemy.ForceKill();
                    GameSessionManager.Instance.AddScore(explosionKillScore);
                }
            }
        }

        private IEnumerator RespawnBossAfterDelay()
        {
            yield return new WaitForSeconds(_bossRespawnDelay);
            SpawnBoss();
        }

        #endregion

        private bool AreSpawnPointsValid()
        {
            if (_spawnPoints.Length > 0) return true;
            Debug.LogError("No spawn points assigned to the spawner.", this);
            return false;
        }

        public bool IsBossActive => _bossIsActive;
        public GameObject CurrentBossInstance => _currentBossInstance;
    }
}