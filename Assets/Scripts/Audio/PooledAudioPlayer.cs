using UnityEngine;
using System.Collections;

namespace EchoMage.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class PooledAudioPlayer : MonoBehaviour, IPoolableObject
    {
        private AudioSource _audioSource;
        private Coroutine _returnCoroutine;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void Play(AudioClip clip, bool is3D)
        {
            _audioSource.clip = clip;
            _audioSource.spatialBlend = is3D ? 1.0f : 0.0f;
            _audioSource.Play();

            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
            }
            _returnCoroutine = StartCoroutine(ReturnToPoolAfterClip(clip.length));
        }

        private IEnumerator ReturnToPoolAfterClip(float duration)
        {
            yield return new WaitForSeconds(duration);
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }

        public void OnObjectSpawn()
        {
            // No specific action needed on spawn
        }

        public void OnObjectReturn()
        {
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            _audioSource.Stop();
            _audioSource.clip = null;
        }
    }
}