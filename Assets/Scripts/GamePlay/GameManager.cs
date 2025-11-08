using EchoMage.Echoes;
using EchoMage.Player;
using EchoMage.UI;
using EchoMage.World;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace EchoMage.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameObject> OnPlayerSpawned;

        [Header("Core References")]
        public PlayerSpawner PlayerSpawner;
        public UIManager UIManager;
        public DespairSystem DespairSystem;

        [Header("Gameplay State")]
        public float WorldThreatLevel = 1f;

        [Header("Echo System")]
        [SerializeField] private GameObject echoGravePrefab;

        private readonly HashSet<GameObject> _activeEnemies = new HashSet<GameObject>();

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
            DespairSystem.UpdateDespair(_activeEnemies.Count, Time.deltaTime);
        }

        public void NotifyPlayerSpawned(GameObject playerInstance)
        {
            PlayerTransform = playerInstance.transform;
            PlayerStats = playerInstance.GetComponent<PlayerStats>();
            OnPlayerSpawned?.Invoke(playerInstance);
        }

        public void HandlePlayerDeath(PlayerStats deadPlayerStats, Vector3 deathPosition)
        {
            if (echoGravePrefab != null)
            {
                CreateEchoGrave(deadPlayerStats, deathPosition);
            }
            PlayerSpawner.RequestRespawn();
        }

        private void CreateEchoGrave(PlayerStats stats, Vector3 position)
        {
            GameObject graveInstance = Instantiate(echoGravePrefab, position, Quaternion.identity);
            var echoData = new PlayerEchoData(stats, position);
            graveInstance.GetComponent<EchoGrave>().Initialize(echoData);
        }

        public void RegisterEnemy(GameObject enemy) => _activeEnemies.Add(enemy);

        public void UnregisterEnemy(GameObject enemy)
        {
            if (_activeEnemies.Remove(enemy))
            {
                DespairSystem.ReduceDespairOnKill();
            }
        }
    }
}