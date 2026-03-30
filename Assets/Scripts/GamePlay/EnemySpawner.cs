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

        [Header("Spawn Control")]
        [Tooltip("Hệ số giảm số lượng quái mỗi wave (0.7 = giảm 30%).")]
        [SerializeField, Range(0.3f, 1f)] private float _spawnCountMultiplier = 0.7f;

        [Tooltip("Số quái tối đa tồn tại cùng lúc trên map.")]
        [SerializeField] private int _maxActiveEnemies = 30;

        [Header("Endless Mode Scaling")]
        [Tooltip("Mỗi khi hoàn thành wave cuối, chỉ số của quái sẽ nhân với giá trị này.")]
        [SerializeField] private float _statMultiplierPerCycle = 1.2f;

        [Header("Boss Configuration")]
        [Tooltip("Boss prefab sẽ spawn sau thời gian quy định.")]
        [SerializeField] private GameObject _bossPrefab;

        [Tooltip("Thời gian (giây) trước khi Boss xuất hiện. Mặc định 600 = 10 phút.")]
        [SerializeField] private float _bossSpawnTime = 600f;

        [Tooltip("Điểm spawn riêng cho Boss (nếu null sẽ dùng spawn point ngẫu nhiên).")]
        [SerializeField] private Transform _bossSpawnPoint;

        [Tooltip("Điểm thưởng khi giết Boss.")]
        [SerializeField] private int _bossKillScore = 500;

        [Tooltip("Bán kính nổ khi Boss chết - tất cả quái trong vùng này sẽ chết.")]
        [SerializeField] private float _bossDeathExplosionRadius = 25f;

        [Tooltip("Thời gian chờ trước khi Boss tiếp theo xuất hiện sau khi Boss bị giết.")]
        [SerializeField] private float _bossRespawnDelay = 120f;

        private int _currentWaveIndex = 0;
        private int _endlessCycleCount = 1;
        private Coroutine _spawnCoroutine;
        private Coroutine _bossTimerCoroutine;

        private GameObject _currentBossInstance;
        private bool _bossIsActive = false;
        private float _gameTimer = 0f;
        private bool _isSpawning = false;

        // Tracking active enemies for max cap
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
            else
            {
                Debug.LogError("Spawner could not start because GameManager or Player was not initialized.", this);
            }
        }

        public void ResetAndRestartWaves()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
            }
            if (_bossTimerCoroutine != null)
            {
                StopCoroutine(_bossTimerCoroutine);
            }

            _currentWaveIndex = 0;
            _endlessCycleCount = 1;
            _gameTimer = 0f;
            _activeEnemyCount = 0;
            _bossIsActive = false;
            _isSpawning = true;

            StartNextWave();

            // Bắt đầu timer cho Boss
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
            float currentThreatMultiplier = Mathf.Pow(_statMultiplierPerCycle, _endlessCycleCount - 1);

            foreach (var entry in wave.WaveEntries)
            {
                // [FIX] Áp dụng hệ số giảm số lượng quái
                int adjustedCount = Mathf.Max(1, Mathf.RoundToInt(entry.Count * _spawnCountMultiplier));

                for (int i = 0; i < adjustedCount; i++)
                {
                    // [FIX] Chờ nếu đã đạt giới hạn quái tối đa
                    while (_activeEnemyCount >= _maxActiveEnemies)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }

                    SpawnEnemy(entry.EnemyPrefab, currentThreatMultiplier);
                    yield return new WaitForSeconds(entry.SpawnInterval);
                }
            }

            yield return new WaitForSeconds(wave.TimeToNextWave);

            _currentWaveIndex++;
            StartNextWave();
        }

        private void SpawnEnemy(GameObject enemyPrefab, float threatMultiplier)
        {
            Transform spawnPoint = _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
            GameObject enemyInstance = ObjectPoolManager.Instance.Spawn(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            if (enemyInstance.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                enemyBase.Initialize(GameManager.Instance.PlayerTransform, threatMultiplier);
            }

            _activeEnemyCount++;
        }

        /// <summary>
        /// Gọi bởi GameManager khi enemy bị unregister (chết hoặc despawn).
        /// </summary>
        public void NotifyEnemyRemoved()
        {
            _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
        }

        #region Boss System

        private IEnumerator BossSpawnTimerRoutine()
        {
            // Chờ đến khi đạt thời gian spawn boss
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
                float currentThreatMultiplier = Mathf.Pow(_statMultiplierPerCycle, _endlessCycleCount - 1);
                boss.Initialize(GameManager.Instance.PlayerTransform, currentThreatMultiplier);
                boss.OnBossDeath += HandleBossDeath;
            }

            _bossIsActive = true;
            OnBossSpawned?.Invoke();
        }

        private void HandleBossDeath(BossEnemy boss)
        {
            boss.OnBossDeath -= HandleBossDeath;
            _bossIsActive = false;

            // Thưởng điểm lớn
            GameSessionManager.Instance.AddScore(_bossKillScore);

            // Giết tất cả quái xung quanh (hiệu ứng nổ)
            KillNearbyEnemies(boss.transform.position, _bossDeathExplosionRadius);

            OnBossKilled?.Invoke();

            // Spawn Boss mới sau delay
            if (_bossTimerCoroutine != null)
            {
                StopCoroutine(_bossTimerCoroutine);
            }
            _bossTimerCoroutine = StartCoroutine(RespawnBossAfterDelay());
        }

        private void KillNearbyEnemies(Vector3 center, float radius)
        {
            // Tìm tất cả enemy trong bán kính
            Collider[] colliders = Physics.OverlapSphere(center, radius);
            int explosionKillScore = 5; // Điểm nhỏ cho mỗi con quái bị nổ

            foreach (var col in colliders)
            {
                if (col.gameObject == _currentBossInstance) continue;

                if (col.TryGetComponent<EnemyBase>(out var enemy))
                {
                    // Spawn hiệu ứng nổ tại vị trí enemy
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

        /// <summary>
        /// Kiểm tra Boss đang active hay không (cho UI hiển thị boss HP bar).
        /// </summary>
        public bool IsBossActive => _bossIsActive;
        public GameObject CurrentBossInstance => _currentBossInstance;
    }
}