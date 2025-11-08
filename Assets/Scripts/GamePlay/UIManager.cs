using EchoMage.Core;
using EchoMage.Player;
using UnityEngine;
using TMPro;

namespace EchoMage.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Component References")]
        // SỬA LỖI: Thay đổi kiểu tham chiếu sang script mới
        [SerializeField] private UIBillProgress healthBar;
        [SerializeField] private UIBillProgress despairBar;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private TextMeshProUGUI gameOverReasonText;

        [Header("Dependencies")]
        [SerializeField] private DespairSystem despairSystem;

        private PlayerHealth _currentPlayerHealth;

        private void OnEnable()
        {
            if (despairSystem != null)
            {
                despairSystem.OnDespairChanged += UpdateDespairBar;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerSpawned += HandlePlayerSpawned;
            }
        }

        private void OnDisable()
        {
            if (despairSystem != null)
            {
                despairSystem.OnDespairChanged -= UpdateDespairBar;
            }

            if (_currentPlayerHealth != null)
            {
                _currentPlayerHealth.OnHealthChanged -= UpdateHealthBar;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerSpawned -= HandlePlayerSpawned;
            }
        }

        private void Start()
        {
            gameOverScreen.SetActive(false);
            if (despairSystem != null)
            {
                despairSystem.InitializeDespair();
            }
        }

        private void HandlePlayerSpawned(GameObject newPlayer)
        {
            if (_currentPlayerHealth != null)
            {
                _currentPlayerHealth.OnHealthChanged -= UpdateHealthBar;
            }

            _currentPlayerHealth = newPlayer.GetComponent<PlayerHealth>();

            if (_currentPlayerHealth != null)
            {
                _currentPlayerHealth.OnHealthChanged += UpdateHealthBar;
                _currentPlayerHealth.InitializeHealth();
            }
        }

        private void UpdateHealthBar(float current, float max) => healthBar?.SetProgress(current, max);

        private void UpdateDespairBar(float current, float max) => despairBar?.SetProgress(current, max);

        public void ShowGameOverScreen(string reason)
        {
            gameOverScreen.SetActive(true);
            gameOverReasonText.text = reason;
        }
    }
}