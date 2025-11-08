using EchoMage.Core;
using EchoMage.Interfaces;
using System;
using UnityEngine;

namespace EchoMage.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        public event Action<float, float> OnHealthChanged;

        private PlayerStats _playerStats;
        private float _currentHealth;
        private bool _isDead = false;

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            InitializeHealth();
        }

        public void TakeDamage(float damageAmount)
        {
            if (_isDead || damageAmount <= 0) return;

            _currentHealth -= damageAmount;
            OnHealthChanged?.Invoke(_currentHealth, _playerStats.MaxHP);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        // SỬA LỖI: Thêm phương thức để các hệ thống khác (như Despair) có thể buộc người chơi phải chết
        public void ForceKill(string reason)
        {
            if (_isDead) return;
            Debug.Log($"Player killed by system: {reason}");
            Die();
        }

        private void Die()
        {
            if (_isDead) return;

            _isDead = true;
            GameManager.Instance.HandlePlayerDeath(_playerStats, transform.position);
            Destroy(gameObject);
        }

        public void InitializeHealth()
        {
            _currentHealth = _playerStats.MaxHP;
            OnHealthChanged?.Invoke(_currentHealth, _playerStats.MaxHP);
        }
    }
}