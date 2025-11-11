using EchoMage.Core;
using EchoMage.Player;
using UnityEngine;
using TMPro;
using System.Collections; // Thêm để sử dụng Coroutine

namespace EchoMage.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Health UI")]
        [SerializeField] private UIBillProgress healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Despair UI")]
        [SerializeField] private UIBillProgress despairBar;
        [SerializeField] private TextMeshProUGUI despairText;

        [Header("State Screens")]
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject continueScreen;
        [SerializeField] private TextMeshProUGUI gameOverReasonText;
        [SerializeField] private GamePlayUI gamePlayUI;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI currentScore;
        [SerializeField] private TextMeshProUGUI highestScoreText;


        [Header("Gameplay Notifications")]
        [SerializeField] private TextMeshProUGUI cycleNotificationText; // Text thông báo cycle mới

        [Header("Dependencies")]
        [SerializeField] private DespairSystem despairSystem;

        private PlayerHealth _currentPlayerHealth;
        private Coroutine _notificationCoroutine;


        private void OnEnable()
        {
            if (despairSystem != null) despairSystem.OnDespairChanged += UpdateDespairUI;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerSpawned += HandlePlayerSpawned;
                // Lắng nghe sự kiện từ Spawner
                if (GameManager.Instance.EnemySpawner != null)
                {
                    GameManager.Instance.EnemySpawner.OnEndlessCycleStarted += HandleNewCycle;
                }
            }
            if (GameSessionManager.Instance == null) return;
            GameSessionManager.Instance.OnScoreUpdated += UpdateScoreDisplay;
            GameSessionManager.Instance.OnHighestScoreUpdated += UpdateHighestScoreDisplay;
        }

        private void OnDisable()
        {
            if (despairSystem != null) despairSystem.OnDespairChanged -= UpdateDespairUI;
            if (_currentPlayerHealth != null) _currentPlayerHealth.OnHealthChanged -= UpdateHealthUI;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerSpawned -= HandlePlayerSpawned;
                if (GameManager.Instance.EnemySpawner != null)
                {
                    GameManager.Instance.EnemySpawner.OnEndlessCycleStarted -= HandleNewCycle;
                }
            }
            if (GameSessionManager.Instance == null) return;
            GameSessionManager.Instance.OnScoreUpdated -= UpdateScoreDisplay;
            GameSessionManager.Instance.OnHighestScoreUpdated -= UpdateHighestScoreDisplay;
        }

        private void UpdateScoreDisplay(int score)
        {
            gamePlayUI.SetCurrentScore(score);
        }

        private void UpdateHighestScoreDisplay(int highestScore)
        {
            gamePlayUI.SetHighestScore(highestScore);
        }

        private void Start()
        {
            gameOverScreen.SetActive(false);
            continueScreen.SetActive(false);
            cycleNotificationText.gameObject.SetActive(false);
            if (despairSystem != null)
            {
                despairSystem.InitializeDespair();
            }
        }

        private void HandleNewCycle(int cycleCount)
        {
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
            }
            _notificationCoroutine = StartCoroutine(ShowCycleNotification(cycleCount));
        }

        private IEnumerator ShowCycleNotification(int cycleCount)
        {
            cycleNotificationText.text = $"Endless Cycle {cycleCount}";
            cycleNotificationText.gameObject.SetActive(true);

            yield return new WaitForSeconds(3.0f); // Hiển thị trong 3 giây

            cycleNotificationText.gameObject.SetActive(false);
        }

        // ... các hàm còn lại không thay đổi ...
        private void HandlePlayerSpawned(GameObject newPlayer)
        {
            if (_currentPlayerHealth != null) _currentPlayerHealth.OnHealthChanged -= UpdateHealthUI;
            if (newPlayer.TryGetComponent<PlayerHealth>(out _currentPlayerHealth))
            {
                _currentPlayerHealth.OnHealthChanged += UpdateHealthUI;
                _currentPlayerHealth.InitializeHealth();
            }
        }

        private void UpdateHealthUI(float current, float max)
        {
            healthBar?.SetProgress(current, max);
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void UpdateDespairUI(float current, float max)
        {
            despairBar?.SetProgress(current, max);
            if (despairText != null) despairText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }

        public void ShowGameOverScreen(string reason)
        {
            gameOverScreen.SetActive(true);
            gameOverReasonText.text = reason;

        }

        public void ShowContinueScreen()
        {
            continueScreen.SetActive(true);
            highestScoreText.text = "Highest Score: " + GameSessionManager.Instance.HighestScore.ToString();
            currentScore.text = "Current Score: " + GameSessionManager.Instance.CurrentScore.ToString();
        }

        public void HideContinueScreen()
        {
            continueScreen.SetActive(false);
        }
    }
}