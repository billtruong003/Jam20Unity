using System.Collections;
using System.Collections.Generic;
using EchoMage.Enemies;
using UnityEngine;
using EchoMage.Core;

namespace EchoMage.Spawning
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private List<WaveData> _waves;
        [SerializeField] private Transform[] _spawnPoints;

        // Giả sử có 1 class để lấy thông tin global
        // [SerializeField] private PlayerReference _player; 
        // [SerializeField] private WorldState _worldState;

        private int _currentWaveIndex = 0;
        private Coroutine _spawnCoroutine;

        private void Start()
        {
            if (_spawnPoints.Length == 0)
            {
                Debug.LogError("No spawn points assigned to the spawner.", this);
                this.enabled = false;
                return;
            }
            if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            {
                StartNextWave();
            }
            else
            {
                Debug.LogError("Spawner không thể bắt đầu vì GameManager hoặc Player chưa được khởi tạo.", this);
            }
            StartNextWave();
        }

        private void StartNextWave()
        {
            if (_currentWaveIndex >= _waves.Count)
            {
                // Đã hoàn thành tất cả các wave
                Debug.Log("All waves completed!");
                return;
            }

            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
            }
            _spawnCoroutine = StartCoroutine(SpawnWave(_waves[_currentWaveIndex]));
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            foreach (var entry in wave.WaveEntries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    SpawnEnemy(entry.EnemyPrefab);
                    yield return new WaitForSeconds(entry.SpawnInterval);
                }
            }

            yield return new WaitForSeconds(wave.TimeToNextWave);

            _currentWaveIndex++;
            StartNextWave();
        }

        private void SpawnEnemy(GameObject enemyPrefab)
        {
            Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

            GameObject enemyInstance = ObjectPoolManager.Instance.Spawn(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            var enemyBase = enemyInstance.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                // Đây là dòng code quan trọng nhất:
                // Lấy mục tiêu và threat level từ GameManager và truyền vào cho kẻ địch.
                enemyBase.Initialize(GameManager.Instance.PlayerTransform, GameManager.Instance.WorldThreatLevel);
            }
        }
    }
}