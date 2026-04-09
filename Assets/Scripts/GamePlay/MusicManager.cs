using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EchoMage.Core
{
    /// <summary>
    /// Quản lý nhạc nền (menu) và nhạc in-game.
    /// 
    /// [QUAN TRỌNG] MusicManager có AudioSource RIÊNG cho nhạc nền.
    /// KHÔNG dùng SoundManager.PlayMusic() vì:
    /// - MusicManager có DontDestroyOnLoad → sống xuyên scene
    /// - SoundManager KHÔNG có DontDestroyOnLoad → chết khi chuyển scene
    /// - Nếu dùng SoundManager để phát nhạc → nhạc đứt mỗi lần chuyển scene
    /// 
    /// SoundManager chỉ lo SFX (tiếng bắn, tiếng nổ, pickup...) trong mỗi scene.
    /// MusicManager tự lo nhạc nền bằng AudioSource gắn trên chính nó.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Music Tracks")]
        [Tooltip("Nhạc nền menu chính.")]
        [SerializeField] private AudioClip menuMusic;

        [Tooltip("Danh sách nhạc in-game (sẽ phát ngẫu nhiên/tuần tự).")]
        [SerializeField] private List<AudioClip> gameplayMusic = new List<AudioClip>();

        [Tooltip("Nhạc khi Boss xuất hiện.")]
        [SerializeField] private AudioClip bossMusic;

        [Tooltip("Nhạc khi game over.")]
        [SerializeField] private AudioClip gameOverMusic;

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 1.5f;

        [Header("Playback Mode")]
        [SerializeField] private bool shufflePlaylist = true;

        // AudioSource RIÊNG của MusicManager — không phụ thuộc SoundManager
        private AudioSource _musicSource;
        private float _musicVolume = 1f;
        private bool _musicEnabled = true;

        // [MỚI] SFX settings — lưu ở MusicManager vì nó sống xuyên scene
        // SoundManager ở play scene sẽ đọc từ đây trong Start()
        private float _sfxVolume = 1f;
        private bool _sfxEnabled = true;

        private int _currentTrackIndex = -1;
        private Coroutine _playlistCoroutine;
        private Coroutine _fadeCoroutine;
        private AudioClip _currentClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tạo AudioSource ngay trên GameObject này — KHÔNG dùng SoundManager
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f; // 2D — nhạc nền không cần 3D
            _musicSource.priority = 0;       // Ưu tiên cao nhất

            // Load volume đã lưu từ PlayerPrefs
            _musicVolume = PlayerPrefs.GetFloat("Settings_MusicVolume", 1f);
            _musicEnabled = PlayerPrefs.GetInt("Settings_MusicEnabled", 1) == 1;
            _musicSource.volume = _musicEnabled ? _musicVolume : 0f;

            // [MỚI] Load SFX settings
            _sfxVolume = PlayerPrefs.GetFloat("Settings_SFXVolume", 1f);
            _sfxEnabled = PlayerPrefs.GetInt("Settings_SFXEnabled", 1) == 1;

            PlayMenuMusic();
        }

        #region Public API

        /// <summary>
        /// Phát nhạc menu chính.
        /// </summary>
        public void PlayMenuMusic()
        {
            StopPlaylist();
            PlayClipWithFade(menuMusic, true);
        }

        /// <summary>
        /// Bắt đầu phát danh sách nhạc gameplay.
        /// </summary>
        public void PlayGameplayMusic()
        {
            if (gameplayMusic == null || gameplayMusic.Count == 0) return;

            StopPlaylist();

            if (shufflePlaylist)
            {
                ShufflePlaylist();
            }

            _currentTrackIndex = 0;
            _playlistCoroutine = StartCoroutine(PlaylistRoutine());
        }

        /// <summary>
        /// Chuyển sang nhạc Boss.
        /// </summary>
        public void PlayBossMusic()
        {
            StopPlaylist();
            PlayClipWithFade(bossMusic, true);
        }

        /// <summary>
        /// Quay lại nhạc gameplay sau khi Boss chết.
        /// </summary>
        public void ResumeGameplayMusic()
        {
            PlayGameplayMusic();
        }

        /// <summary>
        /// Phát nhạc Game Over.
        /// </summary>
        public void PlayGameOverMusic()
        {
            StopPlaylist();
            PlayClipWithFade(gameOverMusic, false);
        }

        /// <summary>
        /// Dừng tất cả nhạc.
        /// </summary>
        public void StopAllMusic()
        {
            StopPlaylist();
            StopFade();
            if (_musicSource.isPlaying)
            {
                _fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
            }
        }

        /// <summary>
        /// Set music volume (0-1). Lưu PlayerPrefs.
        /// Nếu music đang tắt (mute), volume vẫn được lưu nhưng không apply lên AudioSource.
        /// </summary>
        public void SetVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Settings_MusicVolume", _musicVolume);

            if (_musicSource != null)
            {
                _musicSource.volume = _musicEnabled ? _musicVolume : 0f;
            }
        }

        public float GetVolume() => _musicVolume;

        /// <summary>
        /// Bật/tắt Music. Dùng cho toggle mute trong Settings UI.
        /// Khi tắt → volume AudioSource = 0 (nhạc vẫn play, chỉ mute).
        /// Khi bật lại → khôi phục volume đã lưu trước đó.
        /// </summary>
        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            PlayerPrefs.SetInt("Settings_MusicEnabled", enabled ? 1 : 0);

            if (_musicSource != null)
            {
                _musicSource.volume = enabled ? _musicVolume : 0f;
            }
        }

        /// <summary>
        /// Music đang bật hay tắt? Dùng cho toggle UI binding.
        /// </summary>
        public bool IsMusicEnabled() => _musicEnabled;

        // ─────────────────── SFX Settings API ───────────────────
        // MusicManager giữ SFX settings vì nó sống xuyên scene (DontDestroyOnLoad).
        // SoundManager (ở play scene) sẽ đọc từ đây trong Start().

        /// <summary>
        /// Set SFX volume (0-1). Lưu PlayerPrefs luôn.
        /// SoundManager đang active sẽ được cập nhật ngay lập tức.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Settings_SFXVolume", _sfxVolume);

            // Cập nhật SoundManager nếu đang tồn tại trong scene hiện tại
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetSFXVolume(_sfxVolume);
            }
        }

        /// <summary>
        /// Lấy SFX volume hiện tại (0-1).
        /// </summary>
        public float GetSfxVolume() => _sfxVolume;

        /// <summary>
        /// Bật/tắt SFX. Dùng cho toggle button trong Settings UI.
        /// Lưu PlayerPrefs luôn.
        /// </summary>
        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
            PlayerPrefs.SetInt("Settings_SFXEnabled", enabled ? 1 : 0);

            // Cập nhật SoundManager nếu đang tồn tại
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetSFXVolume(enabled ? _sfxVolume : 0f);
            }
        }

        /// <summary>
        /// SFX đang bật hay tắt? Dùng cho toggle UI binding.
        /// </summary>
        public bool IsSfxEnabled() => _sfxEnabled;

        #endregion

        #region Internal Playback

        private void PlayClipWithFade(AudioClip clip, bool loop)
        {
            if (clip == null) return;

            StopFade();

            if (_musicSource.isPlaying && _musicSource.clip != clip)
            {
                _fadeCoroutine = StartCoroutine(CrossfadeRoutine(clip, loop));
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.volume = _musicEnabled ? _musicVolume : 0f;
                _musicSource.Play();
            }

            _currentClip = clip;
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip, bool loop)
        {
            float halfDuration = fadeDuration * 0.5f;

            yield return FadeVolume(0f, halfDuration);

            _musicSource.Stop();
            _musicSource.clip = newClip;
            _musicSource.loop = loop;
            _musicSource.Play();

            yield return FadeVolume(_musicVolume, halfDuration);
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            yield return FadeVolume(0f, duration);
            _musicSource.Stop();
            _musicSource.clip = null;
        }

        private IEnumerator FadeVolume(float targetVolume, float duration)
        {
            float startVolume = _musicSource.volume;
            float timer = 0f;

            while (timer < duration)
            {
                // unscaledDeltaTime để fade vẫn chạy khi game pause (timeScale = 0)
                timer += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }
            _musicSource.volume = targetVolume;
        }

        private void StopFade()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        #endregion

        #region Playlist Logic

        private IEnumerator PlaylistRoutine()
        {
            while (true)
            {
                if (_currentTrackIndex >= gameplayMusic.Count)
                {
                    _currentTrackIndex = 0;
                    if (shufflePlaylist) ShufflePlaylist();
                }

                AudioClip track = gameplayMusic[_currentTrackIndex];
                if (track != null)
                {
                    PlayClipWithFade(track, false);
                    // WaitForSecondsRealtime — không bị freeze khi timeScale = 0
                    yield return new WaitForSecondsRealtime(track.length - fadeDuration);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(1f);
                }

                _currentTrackIndex++;
            }
        }

        private void StopPlaylist()
        {
            if (_playlistCoroutine != null)
            {
                StopCoroutine(_playlistCoroutine);
                _playlistCoroutine = null;
            }
        }

        private void ShufflePlaylist()
        {
            for (int i = gameplayMusic.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (gameplayMusic[i], gameplayMusic[j]) = (gameplayMusic[j], gameplayMusic[i]);
            }
        }

        #endregion
    }
}