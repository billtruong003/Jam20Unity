using EchoMage.Core;
using System;
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

        public void InitializeDespair()
        {
            _currentDespair = 0;
            OnDespairChanged?.Invoke(_currentDespair, maxDespair);
        }

        public void UpdateDespair(int activeEnemyCount, float deltaTime)
        {
            if (activeEnemyCount <= 0) return;

            IncreaseDespairOverTime(activeEnemyCount, deltaTime);
        }

        private void IncreaseDespairOverTime(int enemyCount, float deltaTime)
        {
            _currentDespair = Mathf.Min(_currentDespair + (enemyCount * despairPerEnemyPerSecond * deltaTime), maxDespair);
            OnDespairChanged?.Invoke(_currentDespair, maxDespair);
            CheckForOverwhelm();
        }

        public void ReduceDespairOnKill()
        {
            _currentDespair = Mathf.Max(0, _currentDespair - despairReductionPerKill);
            OnDespairChanged?.Invoke(_currentDespair, maxDespair);
        }

        private void CheckForOverwhelm()
        {
            if (_currentDespair >= maxDespair)
            {
                GameManager.Instance.EndGame("Overwhelmed by despair. The echo fades into nothingness.");
            }
        }
    }
}