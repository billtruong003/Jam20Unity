using System; // Thêm để sử dụng Action
using System.Collections;
using System.Collections.Generic;
using EchoMage.Enemies;
using UnityEngine;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace EchoMage.Spawning
{
    public class EnemySpawner : MonoBehaviour
    {
        public event Action<int> OnEndlessCycleStarted;

        [Header("Wave Configuration")]
        [SerializeField] private List<WaveData> _waves;
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Endless Mode Scaling")]
        [Tooltip("Mỗi khi hoàn thành wave cuối, chỉ số của quái sẽ nhân với giá trị này.")]
        [SerializeField] private float _statMultiplierPerCycle = 1.2f;

        private int _currentWaveIndex = 0;
        private int _endlessCycleCount = 1;
        private Coroutine _spawnCoroutine;

        private void Start()
        {
            if (!AreSpawnPointsValid())
            {
                this.enabled = false;
                return;
            }
            StartCoroutine(InitialSpawnRoutine());
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
            _currentWaveIndex = 0;
            _endlessCycleCount = 1;
            StartNextWave();
        }

        private void StartNextWave()
        {
            if (_currentWaveIndex >= _waves.Count)
            {
                // Logic kích hoạt Endless Mode
                _endlessCycleCount++;
                _currentWaveIndex = _waves.Count - 1; // Quay lại wave cuối cùng
                OnEndlessCycleStarted?.Invoke(_endlessCycleCount);
            }

            _spawnCoroutine = StartCoroutine(SpawnWave(_waves[_currentWaveIndex]));
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            float currentThreatMultiplier = Mathf.Pow(_statMultiplierPerCycle, _endlessCycleCount - 1);

            foreach (var entry in wave.WaveEntries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
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
                // Truyền vào hệ số nhân của endless mode
                enemyBase.Initialize(GameManager.Instance.PlayerTransform, threatMultiplier);
            }
        }

        private bool AreSpawnPointsValid()
        {
            if (_spawnPoints.Length > 0) return true;
            Debug.LogError("No spawn points assigned to the spawner.", this);
            return false;
        }
    }
}