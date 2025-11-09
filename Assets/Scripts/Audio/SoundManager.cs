using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EchoMage.Core
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;

        [Header("SFX Pooling")]
        [SerializeField] private string sfxPlayerPoolId = "SFX_Player";

        private Coroutine _musicFadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlayMusicWithFade(AudioClip musicClip, float fadeDuration = 1.0f, bool loop = true)
        {
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            _musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(musicClip, fadeDuration, loop));
        }

        private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration, bool loop)
        {
            float startVolume = musicSource.volume;
            float timer = 0f;

            while (timer < duration / 2)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2));
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
            musicSource.volume = 0f;
            musicSource.Stop();

            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.Play();

            timer = 0f;
            while (timer < duration / 2)
            {
                musicSource.volume = Mathf.Lerp(0f, startVolume, timer / (duration / 2));
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
            musicSource.volume = startVolume;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            SpawnPooledPlayer(clip, transform.position, false);
        }

        public void PlaySfxAtPosition(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            SpawnPooledPlayer(clip, position, true);
        }

        private void SpawnPooledPlayer(AudioClip clip, Vector3 position, bool is3D)
        {
            GameObject sfxPlayerObject = ObjectPoolManager.Instance.Spawn(sfxPlayerPoolId, position, Quaternion.identity);
            if (sfxPlayerObject.TryGetComponent<PooledAudioPlayer>(out var audioPlayer))
            {
                audioPlayer.Play(clip, is3D);
            }
        }
    }
}