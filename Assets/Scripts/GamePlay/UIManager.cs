using EchoMage.Core;
using EchoMage.Enemies;
using EchoMage.Player;
using UnityEngine;
using TMPro;
using System.Collections;

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

        [Header("Boss UI")]
        [Tooltip("Panel chứa thanh máu Boss (ẩn khi không có Boss).")]
        [SerializeField] private GameObject bossHealthPanel;
        [SerializeField] private UIBillProgress bossHealthBar;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI bossHealthText;

        [Header("State Screens")]
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject continueScreen;
        [SerializeField] private TextMeshProUGUI gameOverReasonText;
        [SerializeField] private GamePlayUI gamePlayUI;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI currentScore;
        [SerializeField] private TextMeshProUGUI highestScoreText;

        [Header("Game Over Score Display")]
        [SerializeField] private TextMeshProUGUI gameOverScoreText;
        [SerializeField] private TextMeshProUGUI gameOverHighScoreText;
        [SerializeField] private TextMeshProUGUI newHighScoreLabel;

        [Header("Gameplay Notifications")]
        [SerializeField] private TextMeshProUGUI cycleNotificationText;
        [SerializeField] private TextMeshProUGUI bossNotificationText;

        [Header("Dependencies")]
        [SerializeField] private DespairSystem despairSystem;

        private PlayerHealth _currentPlayerHealth;
        private BossEnemy _currentBoss;
        private Coroutine _notificationCoroutine;
        private Coroutine _bossNotificationCoroutine;

        private void OnEnable()
        {
            if (despairSystem != null) despairSystem.OnDespairChanged += UpdateDespairUI;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerSpawned += HandlePlayerSpawned;
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
            UnsubscribeBoss();
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

            if (bossHealthPanel != null) bossHealthPanel.SetActive(false);
            if (bossNotificationText != null) bossNotificationText.gameObject.SetActive(false);
            if (newHighScoreLabel != null) newHighScoreLabel.gameObject.SetActive(false);

            if (despairSystem != null)
            {
                despairSystem.InitializeDespair();
            }

            // [FIX] Khởi tạo hiển thị điểm ban đầu
            // GameSessionManager fire events trong Awake/Start TRƯỚC khi UIManager subscribe
            // → UI miss hết giá trị ban đầu → current score hiện sai, highest score hiện 0
            if (GameSessionManager.Instance != null)
            {
                UpdateScoreDisplay(GameSessionManager.Instance.CurrentScore);
                UpdateHighestScoreDisplay(GameSessionManager.Instance.HighestScore);
            }
        }

        #region Player Health

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

        #endregion

        #region Despair

        private void UpdateDespairUI(float current, float max)
        {
            despairBar?.SetProgress(current, max);
            if (despairText != null) despairText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }

        #endregion

        #region Boss Health Bar

        /// <summary>
        /// Hiển thị thanh máu Boss trên HUD gameplay.
        /// </summary>
        public void ShowBossHealthBar(GameObject bossInstance)
        {
            if (bossInstance == null || bossHealthPanel == null) return;

            UnsubscribeBoss();

            if (bossInstance.TryGetComponent<BossEnemy>(out _currentBoss))
            {
                _currentBoss.OnBossHealthChanged += UpdateBossHealthUI;
                bossHealthPanel.SetActive(true);

                if (bossNameText != null)
                    bossNameText.text = "BOSS";

                // Hiển thị notification
                ShowBossNotification("A BOSS HAS APPEARED!");
            }
        }

        /// <summary>
        /// Ẩn thanh máu Boss.
        /// </summary>
        public void HideBossHealthBar()
        {
            UnsubscribeBoss();

            if (bossHealthPanel != null)
                bossHealthPanel.SetActive(false);

            ShowBossNotification("BOSS DEFEATED!");
        }

        private void UpdateBossHealthUI(float current, float max)
        {
            bossHealthBar?.SetProgress(current, max);
            if (bossHealthText != null)
                bossHealthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void UnsubscribeBoss()
        {
            if (_currentBoss != null)
            {
                _currentBoss.OnBossHealthChanged -= UpdateBossHealthUI;
                _currentBoss = null;
            }
        }

        private void ShowBossNotification(string message)
        {
            if (bossNotificationText == null) return;

            if (_bossNotificationCoroutine != null)
                StopCoroutine(_bossNotificationCoroutine);

            _bossNotificationCoroutine = StartCoroutine(ShowBossNotificationRoutine(message));
        }

        private IEnumerator ShowBossNotificationRoutine(string message)
        {
            bossNotificationText.text = message;
            bossNotificationText.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            bossNotificationText.gameObject.SetActive(false);
        }

        #endregion

        #region Cycle Notifications

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
            yield return new WaitForSeconds(3.0f);
            cycleNotificationText.gameObject.SetActive(false);
        }

        #endregion

        #region Game State Screens

        public void ShowGameOverScreen(string reason)
        {
            gameOverScreen.SetActive(true);
            gameOverReasonText.text = reason;

            // [MỚI] Hiển thị điểm chi tiết
            if (GameSessionManager.Instance != null)
            {
                int finalScore = GameSessionManager.Instance.CurrentScore;
                int highScore = GameSessionManager.Instance.HighestScore;

                if (gameOverScoreText != null)
                    gameOverScoreText.text = $"Score: {finalScore}";

                if (gameOverHighScoreText != null)
                    gameOverHighScoreText.text = $"Best: {highScore}";

                // Hiển thị "NEW HIGH SCORE!" nếu đạt điểm cao mới
                if (newHighScoreLabel != null)
                    newHighScoreLabel.gameObject.SetActive(finalScore >= highScore && finalScore > 0);
            }
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

        #endregion
    }
}