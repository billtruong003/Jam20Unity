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

        private void Start() => InitializeDespair();

        public void UpdateDespair(int activeEnemyCount, float deltaTime)
        {
            if (activeEnemyCount > 0)
            {
                IncreaseDespairOverTime(activeEnemyCount, deltaTime);
            }
        }

        private void IncreaseDespairOverTime(int enemyCount, float deltaTime)
        {
            _currentDespair += enemyCount * despairPerEnemyPerSecond * deltaTime;
            _currentDespair = Mathf.Min(_currentDespair, maxDespair);
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
                // SỬA LỖI: Thay vì kết thúc game, tìm người chơi hiện tại và buộc họ phải chết.
                Transform playerTransform = GameManager.Instance.PlayerTransform;
                if (playerTransform != null && playerTransform.TryGetComponent<PlayerHealth>(out var playerHealth))
                {
                    playerHealth.ForceKill("Overwhelmed by despair.");
                    // Đặt lại Despair ngay sau khi giết người chơi để tránh gọi lại liên tục
                    InitializeDespair();
                }
            }
        }

        public void InitializeDespair()
        {
            _currentDespair = 0;
            OnDespairChanged?.Invoke(_currentDespair, maxDespair);
        }
    }
}