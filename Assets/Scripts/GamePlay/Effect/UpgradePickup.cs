using UnityEngine;
using EchoMage.Core;
using BillUtils.ObjectPooler;
using EchoMage.Loot.Effects; // Namespace mới của chúng ta

namespace EchoMage.Loot
{
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour, IPoolableObject // Giữ lại IPoolableObject để tối ưu
    {
        [Tooltip("Hiệu ứng sẽ được áp dụng khi nhặt vật phẩm này.")]
        [SerializeField] private PickupEffect effect;

        [Header("Visual & Audio Feedback")]
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField] private AudioClip pickupSound;

        [Header("Attraction Mechanics")]
        [SerializeField] private float attractionRadius = 5f;
        [SerializeField] private float attractionSpeed = 8f;

        private Transform _playerTransform;
        private bool _isAttracted = false;
        private bool _isAlive = true;

        private void OnEnable()
        {
            FindPlayerReference();
        }

        private void Update()
        {
            if (!_isAlive || _playerTransform == null) return;

            if (!_isAttracted)
            {
                CheckForAttraction();
            }
            else
            {
                MoveTowardsPlayer();
            }
        }

        private void FindPlayerReference()
        {
            if (GameManager.Instance != null)
            {
                _playerTransform = GameManager.Instance.PlayerTransform;
            }
        }

        private void CheckForAttraction()
        {
            if ((transform.position - _playerTransform.position).sqrMagnitude <= attractionRadius * attractionRadius)
            {
                _isAttracted = true;
            }
        }

        private void MoveTowardsPlayer()
        {
            transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, attractionSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isAlive || !other.CompareTag("Player") || effect == null) return;

            effect.Apply(other.gameObject);
            TriggerFeedbackEffects();
            ReturnToPool();
        }

        private void TriggerFeedbackEffects()
        {
            if (pickupEffectPrefab != null && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Spawn(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            if (pickupSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(pickupSound, this.transform.position);
            }
        }

        private void ReturnToPool()
        {
            _isAlive = false;
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        public void OnObjectSpawn()
        {
            _isAlive = true;
            _isAttracted = false;
            FindPlayerReference();
        }

        public void OnObjectReturn()
        {
            _playerTransform = null;
        }
    }
}