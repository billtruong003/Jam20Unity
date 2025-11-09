using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace EchoMage.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        #region Config

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;

        [Header("SFX Pooling")]
        [SerializeField] private string sfxPlayerPoolId = "SFX_Player";
        [SerializeField, Min(1)] private int prewarmSFXCount = 5;

        [Header("Default Settings")]
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 1f;

        [Header("Pitch Randomization")]
        [SerializeField] private Vector2 sfxPitchRange = new Vector2(0.9f, 1.1f);

        #endregion

        #region Private Fields

        private Coroutine _musicFadeCoroutine;
        private float _masterMusicVolume = 1f;
        private float _masterSFXVolume = 1f;

        private readonly Dictionary<AudioClip, AudioSource> _activeSFXSources = new();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            PrewarmSFXPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Initialization

        private void InitializeAudioSources()
        {
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = defaultMusicVolume;

            _masterMusicVolume = defaultMusicVolume;
            _masterSFXVolume = defaultSFXVolume;
        }

        private void PrewarmSFXPool()
        {
            if (prewarmSFXCount <= 0) return;

            for (int i = 0; i < prewarmSFXCount; i++)
            {
                var obj = ObjectPoolManager.Instance.Spawn(sfxPlayerPoolId, Vector3.zero, Quaternion.identity, null, false);
                if (obj != null) ObjectPoolManager.Instance.Despawn(obj);
            }
        }

        #endregion

        #region Music Control

        public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)
        {
            if (clip == null || musicSource == null) return;

            StopMusicFade();
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = volume * _masterMusicVolume;
            musicSource.Play();
        }

        public void PlayMusicWithFade(AudioClip clip, float fadeDuration = 1f, bool loop = true)
        {
            if (clip == null) return;
            StopMusicFade();
            _musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, fadeDuration, loop));
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            if (musicSource == null || !musicSource.isPlaying) return;
            StopMusicFade();
            _musicFadeCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
        }

        public void PauseMusic() => musicSource?.Pause();
        public void ResumeMusic() => musicSource?.UnPause();

        private void StopMusicFade()
        {
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
                _musicFadeCoroutine = null;
            }
        }

        private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration, bool loop)
        {
            float halfDuration = duration * 0.5f;
            float startVolume = musicSource.volume;

            // Fade out
            yield return StartCoroutine(FadeVolume(0f, halfDuration));

            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.Play();

            // Fade in
            yield return StartCoroutine(FadeVolume(startVolume, halfDuration));
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            yield return StartCoroutine(FadeVolume(0f, duration));
            musicSource.Stop();
            musicSource.clip = null;
        }

        private IEnumerator FadeVolume(float targetVolume, float duration)
        {
            float startVolume = musicSource.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }
            musicSource.volume = targetVolume;
        }

        #endregion

        #region SFX Control

        public void PlaySfx(AudioClip clip, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            bool is3D = position.HasValue;
            Vector3 pos = position ?? transform.position;

            var playerObj = ObjectPoolManager.Instance.Spawn(sfxPlayerPoolId, pos, Quaternion.identity);
            if (playerObj != null && playerObj.TryGetComponent<PooledAudioPlayer>(out var audioPlayer))
            {
                float finalVolume = volume * _masterSFXVolume;
                audioPlayer.Play(clip, is3D, finalVolume, pitch);
                TrackActiveSFX(clip, audioPlayer.Source);
            }
        }

        public void PlaySfxRandomPitch(AudioClip clip, Vector3? position = null, float volume = 1f)
        {
            float randomPitch = Random.Range(sfxPitchRange.x, sfxPitchRange.y);
            PlaySfx(clip, position, volume, randomPitch);
        }

        public void PlaySfx2D(AudioClip clip, float volume = 1f, float pitch = 1f)
            => PlaySfx(clip, null, volume, pitch);

        public void StopAllSFX()
        {
            foreach (var kvp in _activeSFXSources)
            {
                if (kvp.Value != null && kvp.Value.isPlaying)
                    kvp.Value.Stop();
            }
            _activeSFXSources.Clear();
        }

        public void StopSFX(AudioClip clip)
        {
            if (_activeSFXSources.TryGetValue(clip, out var source) && source != null)
            {
                source.Stop();
                _activeSFXSources.Remove(clip);
            }
        }

        private void TrackActiveSFX(AudioClip clip, AudioSource source)
        {
            // Ghi đè nếu đã tồn tại (cho phép nhiều cùng lúc)
            _activeSFXSources[clip] = source;

            // Tự động xóa khi kết thúc
            StartCoroutine(RemoveWhenDone(source, clip));
        }

        private IEnumerator RemoveWhenDone(AudioSource source, AudioClip clip)
        {
            while (source != null && source.isPlaying)
                yield return null;

            _activeSFXSources.Remove(clip);
        }

        #endregion

        #region Volume Control

        public void SetMusicVolume(float volume)
        {
            _masterMusicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = _masterMusicVolume * defaultMusicVolume;
        }

        public void SetSFXVolume(float volume)
        {
            _masterSFXVolume = Mathf.Clamp01(volume);
            // Áp dụng cho tất cả SFX đang phát
            foreach (var source in _activeSFXSources.Values)
            {
                if (source != null)
                    source.volume = source.volume / defaultSFXVolume * (_masterSFXVolume * defaultSFXVolume);
            }
        }

        public float GetMusicVolume() => _masterMusicVolume;
        public float GetSFXVolume() => _masterSFXVolume;

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Play Test SFX")]
        private void Editor_PlayTestSFX()
        {
            if (Application.isPlaying)
                PlaySfx2D(Resources.Load<AudioClip>("TestSFX"));
        }

        [ContextMenu("Prewarm Pool")]
        private void Editor_Prewarm() => PrewarmSFXPool();
#endif

        #endregion
    }
}