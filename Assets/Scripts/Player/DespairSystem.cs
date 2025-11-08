// Assets/Scripts/GamePlay/DespairSystem.cs

using EchoMage.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoMage.Player
{
    public sealed class DespairSystem : MonoBehaviour
    {
        public event Action<float, float> OnDespairChanged;

        [Header("Despair Mechanics")]
        [SerializeField] private float maxDespair = 1000f;
        [SerializeField] private float despairPerEnemyPerSecond = 0.5f;
        [SerializeField] private float despairReductionPerKill = 10f;

        private float _currentDespair;
        private readonly HashSet<GameObject> _activeEnemies = new HashSet<GameObject>();

        public void RegisterEnemy(GameObject enemy) => _activeEnemies.Add(enemy);
        public void UnregisterEnemy(GameObject enemy)
        {
            _activeEnemies.Remove(enemy);
            ReduceDespairOnKill();
        }

        private void Start()
        {
            InitializeDespair();
        }

        private void Update()
        {
            if (_activeEnemies.Count > 0)
            {
                IncreaseDespairOverTime(Time.deltaTime);
            }
        }

        private void IncreaseDespairOverTime(float deltaTime)
        {
            _currentDespair += _activeEnemies.Count * despairPerEnemyPerSecond * deltaTime;
            CheckForOverwhelm();
            OnDespairChanged?.Invoke(_currentDespair, maxDespair);
        }

        private void ReduceDespairOnKill()
        {
            _currentDespair = Mathf.Max(0, _currentDespair - despairReductionPerKill);
            OnDespairChanged?.Invoke(_currentDespair, maxDespair);
        }

        private void CheckForOverwhelm()
        {
            if (_currentDespair >= maxDespair)
            {
                _currentDespair = maxDespair;
                GameManager.Instance.EndGame("Overwhelmed by the darkness.");
            }
        }

        public void InitializeDespair()
        {
            _currentDespair = 0;
            OnDespairChanged?.Invoke(_currentDespair, maxDespair);
        }
    }
}