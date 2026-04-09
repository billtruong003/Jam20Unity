using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using CleanCode.SceneManagement;

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
        [Tooltip("Panel chính (overlay/background). Chứa cả subPause và settings bên trong.")]
        [SerializeField] private GameObject pauseMenuPanel;
        [Tooltip("Panel con chứa các nút: Resume, Settings, Back to Menu.")]
        [SerializeField] private GameObject subPausePanel;
        [Tooltip("Panel settings chứa sliders volume, toggles mute. Nằm trong pauseMenuPanel.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Back to Menu")]
        [Tooltip("Tên scene main menu (phải trùng tên trong Build Settings).")]
        [SerializeField] private string menuSceneName = "MainMenu";
        [Tooltip("CanvasGroup cho fade transition khi chuyển scene (optional).")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        public bool IsPaused { get; private set; } = false;
        private bool _isGameOver = false;
        private bool _isInSettings = false;

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
            if (subPausePanel != null) subPausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void Update()
        {
            if (_isGameOver) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Ưu tiên 1: Đang ở settings → về lại sub pause
                if (_isInSettings)
                {
                    CloseSettings();
                    return;
                }

                // Ưu tiên 2: Đang pause (sub pause hiện) → resume chơi tiếp
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
            _isInSettings = false;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            // Hiện main overlay + sub pause buttons, ẩn settings
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            if (subPausePanel != null) subPausePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            OnPauseStateChanged?.Invoke(true);
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;

            IsPaused = false;
            _isInSettings = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            // Ẩn hết — main panel ẩn thì children cũng ẩn theo
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (subPausePanel != null) subPausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Mở Settings panel (gọi từ nút "Settings" trên sub pause panel).
        /// Ẩn sub pause buttons, hiện settings. Main panel (overlay) giữ nguyên.
        /// </summary>
        public void OpenSettings()
        {
            _isInSettings = true;

            if (subPausePanel != null) subPausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        /// <summary>
        /// Đóng Settings, quay lại sub pause panel.
        /// Gọi từ nút "Back" trong Settings hoặc khi nhấn Esc ở settings.
        /// </summary>
        public void CloseSettings()
        {
            _isInSettings = false;

            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (subPausePanel != null) subPausePanel.SetActive(true);
        }

        /// <summary>
        /// Về Main Menu. Gọi từ nút "Back to Menu" trong pause menu.
        /// Reset toàn bộ game state trước khi chuyển scene.
        /// </summary>
        public void BackToMainMenu()
        {
            // Reset time + audio trước khi chuyển scene
            IsPaused = false;
            _isGameOver = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            // Đóng tất cả panel
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Chuyển scene qua SceneLoader (có fade nếu có fadeCanvasGroup)
            if (SceneLoader.Instance != null)
            {
                var request = new SceneLoadRequest(menuSceneName, LoadSceneMode.Single, fadeCanvasGroup, fadeDuration);
                SceneLoader.Instance.LoadScene(request);
            }
            else
            {
                // Fallback nếu không có SceneLoader
                SceneManager.LoadScene(menuSceneName);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SETTINGS OPEN API — Gắn vào Slider/Toggle OnValueChanged trong Inspector
        //  UI gọi PauseManager → PauseManager delegate xuống SettingsManager
        //  Mày không cần reference SettingsManager trong UI, chỉ cần PauseManager
        // ═══════════════════════════════════════════════════════════

        #region Settings API — Volume

        /// <summary>
        /// Gắn vào Slider.OnValueChanged(float) cho music volume.
        /// </summary>
        public void OnMusicVolumeChanged(float volume)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetMusicVolume(volume);
        }

        /// <summary>
        /// Gắn vào Slider.OnValueChanged(float) cho SFX volume.
        /// </summary>
        public void OnSfxVolumeChanged(float volume)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetSFXVolume(volume);
        }

        #endregion

        #region Settings API — Toggle Mute

        /// <summary>
        /// Gắn vào Toggle.OnValueChanged(bool) cho music mute.
        /// true = bật music, false = tắt music.
        /// </summary>
        public void OnMusicToggleChanged(bool isOn)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetMusicEnabled(isOn);
        }

        /// <summary>
        /// Gắn vào Toggle.OnValueChanged(bool) cho SFX mute.
        /// true = bật SFX, false = tắt SFX.
        /// </summary>
        public void OnSfxToggleChanged(bool isOn)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetSFXEnabled(isOn);
        }

        #endregion

        #region Settings API — Getters (gọi trong code để init UI state)

        /// <summary>
        /// Lấy music volume hiện tại (0-1). Dùng để set slider.value khi mở settings.
        /// </summary>
        public float GetMusicVolume()
        {
            return SettingsManager.Instance != null ? SettingsManager.Instance.MusicVolume : 1f;
        }

        /// <summary>
        /// Lấy SFX volume hiện tại (0-1). Dùng để set slider.value khi mở settings.
        /// </summary>
        public float GetSfxVolume()
        {
            return SettingsManager.Instance != null ? SettingsManager.Instance.SFXVolume : 1f;
        }

        /// <summary>
        /// Music đang bật hay tắt? Dùng để set toggle.isOn khi mở settings.
        /// </summary>
        public bool GetMusicEnabled()
        {
            return SettingsManager.Instance != null ? SettingsManager.Instance.IsMusicEnabled : true;
        }

        /// <summary>
        /// SFX đang bật hay tắt? Dùng để set toggle.isOn khi mở settings.
        /// </summary>
        public bool GetSfxEnabled()
        {
            return SettingsManager.Instance != null ? SettingsManager.Instance.IsSFXEnabled : true;
        }

        #endregion

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