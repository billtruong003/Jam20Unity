using System;
using UnityEngine;

namespace EchoMage.Core
{
    /// <summary>
    /// Quản lý Pause/Resume game khi ấn ESC.
    /// Hiển thị menu tạm dừng với options: Resume, Settings, Quit.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        public event Action<bool> OnPauseStateChanged;

        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;

        public bool IsPaused { get; private set; } = false;
        private bool _isGameOver = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void Update()
        {
            if (_isGameOver) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Nếu settings đang mở → đóng settings, quay lại pause menu
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                    return;
                }

                // Toggle pause
                if (IsPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }

        public void PauseGame()
        {
            if (IsPaused || _isGameOver) return;

            IsPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);

            OnPauseStateChanged?.Invoke(true);
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;

            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Mở Settings panel (gọi từ nút "Settings" trong pause menu).
        /// </summary>
        public void OpenSettings()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        /// <summary>
        /// Đóng Settings, quay lại Pause menu.
        /// </summary>
        public void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Gọi khi game over để ngăn ESC hoạt động.
        /// </summary>
        public void SetGameOverState(bool isGameOver)
        {
            _isGameOver = isGameOver;
            if (isGameOver && IsPaused)
            {
                ResumeGame();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                // Đảm bảo timeScale về lại 1 khi destroy
                Time.timeScale = 1f;
                AudioListener.pause = false;
                Instance = null;
            }
        }
    }
}