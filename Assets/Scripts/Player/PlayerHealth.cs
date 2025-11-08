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
        public event Action OnDeath;

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
                Die("You have fallen.");
            }
        }

        public void Die(string reason)
        {
            if (_isDead) return;

            _isDead = true;
            Debug.Log($"Player Died: {reason}");
            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }

        private void InitializeHealth()
        {
            _currentHealth = _playerStats.MaxHP;
            OnHealthChanged?.Invoke(_currentHealth, _playerStats.MaxHP);
        }
    }
}