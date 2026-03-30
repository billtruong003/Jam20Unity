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

            // Load volume đã lưu từ PlayerPrefs (nếu SettingsManager chưa init)
            _musicVolume = PlayerPrefs.GetFloat("Settings_MusicVolume", 1f);
            _musicSource.volume = _musicVolume;
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
        /// Được gọi bởi SettingsManager khi user thay đổi music volume.
        /// KHÔNG gọi qua SoundManager nữa — MusicManager tự quản lý volume riêng.
        /// </summary>
        public void SetVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume;
            }
        }

        public float GetVolume() => _musicVolume;

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
                _musicSource.volume = _musicVolume;
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