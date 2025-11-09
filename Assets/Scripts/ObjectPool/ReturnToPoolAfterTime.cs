// Filename: ReturnToPoolAfterTime.cs
using UnityEngine;
using System.Collections;

namespace YourProject.ObjectPooling
{
    [AddComponentMenu("Pooling/Return To Pool After Time")]
    public sealed class ReturnToPoolAfterTime : MonoBehaviour
    {
        [Tooltip("Thời gian (giây) đối tượng tồn tại trước khi được trả về pool.")]
        [SerializeField]
        private float lifeTime = 2.0f;

        private Coroutine _returnCoroutine;

        private void OnEnable()
        {
            StartReturnCoroutine();
        }

        private void OnDisable()
        {
            StopReturnCoroutine();
        }

        private void StartReturnCoroutine()
        {
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
            }
            _returnCoroutine = StartCoroutine(ReturnAfterDelay());
        }

        private void StopReturnCoroutine()
        {
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
        }

        private IEnumerator ReturnAfterDelay()
        {
            yield return new WaitForSeconds(lifeTime);
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}