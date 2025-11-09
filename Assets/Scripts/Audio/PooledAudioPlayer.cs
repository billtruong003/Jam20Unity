using UnityEngine;
using System.Collections;
using EchoMage.Core;
using BillUtils.ObjectPooler; // Đảm bảo namespace đúng

namespace EchoMage.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class PooledAudioPlayer : MonoBehaviour, IPoolableObject
    {
        public AudioSource Source { get; private set; }

        private Coroutine _returnCoroutine;
        private float _originalPitch = 1f;

        private void Awake()
        {
            Source = GetComponent<AudioSource>();
            Source.playOnAwake = false;
            _originalPitch = Source.pitch;
        }

        /// <summary>
        /// Phát âm thanh với các tùy chọn đầy đủ
        /// </summary>
        public void Play(AudioClip clip, bool is3D, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[PooledAudioPlayer] AudioClip là null!");
                ObjectPoolManager.Instance.Despawn(gameObject);
                return;
            }

            // Cấu hình AudioSource
            Source.clip = clip;
            Source.spatialBlend = is3D ? 1f : 0f;
            Source.volume = volume;
            Source.pitch = pitch;

            // Phát
            Source.Play();

            // Tự động trả về pool sau khi kết thúc
            StopReturnCoroutine();
            float duration = clip.length / Mathf.Abs(pitch); // Điều chỉnh theo pitch
            _returnCoroutine = StartCoroutine(ReturnToPoolAfterClip(duration));
        }

        /// <summary>
        /// Dừng và trả về pool ngay lập tức
        /// </summary>
        public void StopAndDespawn()
        {
            StopReturnCoroutine();
            Source.Stop();
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        private void StopReturnCoroutine()
        {
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
        }

        private IEnumerator ReturnToPoolAfterClip(float duration)
        {
            // Đợi âm thanh phát xong (dùng unscaled để không bị ảnh hưởng Time.timeScale)
            float timer = 0f;
            while (timer < duration && Source != null && Source.isPlaying)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            // Nếu vẫn đang phát (do pause, v.v.), dừng trước khi despawn
            if (Source != null && Source.isPlaying)
                Source.Stop();

            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        #region IPoolableObject

        public void OnObjectSpawn()
        {
            // Reset trạng thái khi được spawn
            StopReturnCoroutine();
            Source.Stop();
            Source.clip = null;
            Source.pitch = _originalPitch;
            Source.volume = 1f;
        }

        public void OnObjectReturn()
        {
            // Dọn dẹp khi trả về pool
            StopReturnCoroutine();
            if (Source != null)
            {
                Source.Stop();
                Source.clip = null;
            }
        }

        #endregion

        #region Editor & Debug

#if UNITY_EDITOR
        [ContextMenu("Play Test Clip")]
        private void Editor_PlayTest()
        {
            if (Application.isPlaying && Source.clip != null)
                Play(Source.clip, false, 1f, 1f);
        }
#endif

        #endregion
    }
}