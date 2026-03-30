using EchoMage.Core;
using UnityEngine;
using System.Collections;

namespace EchoMage.Player
{
    public sealed class PlayerSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform initialSpawnPoint;
        [SerializeField] private float respawnDelay = 3.0f;

        private void Start()
        {
            if (playerPrefab == null || initialSpawnPoint == null)
            {
                enabled = false;
                return;
            }
            InitialSpawn();
        }

        public void RequestRespawn()
        {
            StartCoroutine(RespawnCoroutine());
        }

        private void InitialSpawn()
        {
            SpawnPlayer();
        }

        private IEnumerator RespawnCoroutine()
        {
            // [FIX] Dùng WaitForSecondsRealtime thay vì WaitForSeconds
            // vì khi player chết, Time.timeScale có thể = 0 (HandlePlayerDeath set timeScale = 0)
            // WaitForSeconds sẽ KHÔNG chạy khi timeScale = 0, gây bug player không respawn
            yield return new WaitForSecondsRealtime(respawnDelay);
            SpawnPlayer();
        }

        private void SpawnPlayer()
        {
            GameObject newPlayerInstance = Instantiate(playerPrefab, initialSpawnPoint.position, initialSpawnPoint.rotation);
            GameManager.Instance.NotifyPlayerSpawned(newPlayerInstance);
        }
    }
}