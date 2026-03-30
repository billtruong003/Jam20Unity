using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoMage.Core
{
    /// <summary>
    /// Quản lý cài đặt game: âm lượng nhạc/SFX, keybind.
    /// Lưu trữ bằng PlayerPrefs.
    /// 
    /// [QUAN TRỌNG] Routing volume:
    /// - Music Volume → gọi MusicManager.SetVolume() (MusicManager có AudioSource riêng)
    /// - SFX Volume   → gọi SoundManager.SetSFXVolume() (SoundManager quản lý SFX pool)
    /// 
    /// KHÔNG gọi SoundManager.SetMusicVolume() nữa vì SoundManager không quản lý nhạc nền.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        #region Events
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<string, KeyCode> OnKeybindChanged;
        #endregion

        #region PlayerPrefs Keys
        private const string MUSIC_VOLUME_KEY = "Settings_MusicVolume";
        private const string SFX_VOLUME_KEY = "Settings_SFXVolume";
        private const string KEYBIND_PREFIX = "Settings_Keybind_";
        #endregion

        #region Default Keybinds
        [Serializable]
        public class KeybindEntry
        {
            public string ActionName;
            public KeyCode DefaultKey;
            [HideInInspector] public KeyCode CurrentKey;
        }

        [Header("Keybind Configuration")]
        [SerializeField]
        private List<KeybindEntry> _defaultKeybinds = new List<KeybindEntry>()
        {
            new KeybindEntry { ActionName = "Shoot", DefaultKey = KeyCode.Mouse0 },
            new KeybindEntry { ActionName = "Interact", DefaultKey = KeyCode.E },
            new KeybindEntry { ActionName = "AbsorbPower", DefaultKey = KeyCode.Q },
            new KeybindEntry { ActionName = "Pause", DefaultKey = KeyCode.Escape },
        };
        #endregion

        private Dictionary<string, KeyCode> _keybinds = new Dictionary<string, KeyCode>();
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        #region Properties
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllSettings();
        }

        #region Audio Settings

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, _musicVolume);
            PlayerPrefs.Save();

            // [FIX] Music volume → MusicManager (có AudioSource riêng, DontDestroyOnLoad)
            // KHÔNG gọi SoundManager.SetMusicVolume() — SoundManager không quản lý nhạc nền
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetVolume(_musicVolume);
            }

            OnMusicVolumeChanged?.Invoke(_musicVolume);
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, _sfxVolume);
            PlayerPrefs.Save();

            // SFX volume → SoundManager (quản lý SFX pool, scene-specific)
            // Có thể null khi đang ở giữa scene transition — an toàn vì chỉ là SFX
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetSFXVolume(_sfxVolume);
            }

            OnSFXVolumeChanged?.Invoke(_sfxVolume);
        }

        #endregion

        #region Keybind Settings

        public KeyCode GetKeybind(string actionName)
        {
            return _keybinds.TryGetValue(actionName, out var key) ? key : KeyCode.None;
        }

        public void SetKeybind(string actionName, KeyCode newKey)
        {
            foreach (var kvp in _keybinds)
            {
                if (kvp.Key != actionName && kvp.Value == newKey)
                {
                    KeyCode oldKey = _keybinds[actionName];
                    _keybinds[kvp.Key] = oldKey;
                    SaveKeybind(kvp.Key, oldKey);
                    OnKeybindChanged?.Invoke(kvp.Key, oldKey);
                    break;
                }
            }

            _keybinds[actionName] = newKey;
            SaveKeybind(actionName, newKey);
            OnKeybindChanged?.Invoke(actionName, newKey);
        }

        public void ResetKeybindsToDefault()
        {
            foreach (var entry in _defaultKeybinds)
            {
                _keybinds[entry.ActionName] = entry.DefaultKey;
                SaveKeybind(entry.ActionName, entry.DefaultKey);
                OnKeybindChanged?.Invoke(entry.ActionName, entry.DefaultKey);
            }
        }

        public bool GetActionDown(string actionName)
        {
            if (_keybinds.TryGetValue(actionName, out var key))
            {
                return Input.GetKeyDown(key);
            }
            return false;
        }

        public bool GetAction(string actionName)
        {
            if (_keybinds.TryGetValue(actionName, out var key))
            {
                return Input.GetKey(key);
            }
            return false;
        }

        public bool GetActionUp(string actionName)
        {
            if (_keybinds.TryGetValue(actionName, out var key))
            {
                return Input.GetKeyUp(key);
            }
            return false;
        }

        public Dictionary<string, KeyCode> GetAllKeybinds()
        {
            return new Dictionary<string, KeyCode>(_keybinds);
        }

        #endregion

        #region Save/Load

        private void LoadAllSettings()
        {
            _musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

            _keybinds.Clear();
            foreach (var entry in _defaultKeybinds)
            {
                string saved = PlayerPrefs.GetString(KEYBIND_PREFIX + entry.ActionName, "");
                if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out KeyCode loadedKey))
                {
                    _keybinds[entry.ActionName] = loadedKey;
                    entry.CurrentKey = loadedKey;
                }
                else
                {
                    _keybinds[entry.ActionName] = entry.DefaultKey;
                    entry.CurrentKey = entry.DefaultKey;
                }
            }
        }

        private void SaveKeybind(string actionName, KeyCode key)
        {
            PlayerPrefs.SetString(KEYBIND_PREFIX + actionName, key.ToString());
            PlayerPrefs.Save();
        }

        public void ResetAllSettings()
        {
            SetMusicVolume(1f);
            SetSFXVolume(1f);
            ResetKeybindsToDefault();
        }

        #endregion
    }
}