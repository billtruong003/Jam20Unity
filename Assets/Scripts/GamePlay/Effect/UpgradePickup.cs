using UnityEngine;
using EchoMage.Core;
using BillUtils.ObjectPooler;
using EchoMage.Loot.Effects;
using System.Collections;

namespace EchoMage.Loot
{
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour, IPoolableObject
    {
        [Tooltip("Hiệu ứng sẽ được áp dụng khi nhặt vật phẩm này.")]
        [SerializeField] private PickupEffect effect;

        [Header("Visual & Audio Feedback")]
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField] private AudioClip pickupSound;

        [Header("Attraction Mechanics")]
        [SerializeField] private float attractionRadius = 5f;
        [SerializeField] private float attractionSpeed = 8f;

        [Header("Lifetime")]
        [Tooltip("Thời gian tồn tại (giây) trước khi tự despawn. 0 = vô hạn.")]
        [SerializeField] private float lifetime = 15f;

        [Tooltip("Bắt đầu nhấp nháy bao nhiêu giây trước khi biến mất.")]
        [SerializeField] private float blinkWarningTime = 3f;

        [Tooltip("Tốc độ nhấp nháy (lần/giây).")]
        [SerializeField] private float blinkRate = 6f;

        private Transform _playerTransform;
        private bool _isAttracted = false;
        private bool _isAlive = true;
        private float _spawnTime;
        private Renderer _renderer;
        private bool _isBlinking = false;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        private void Update()
        {
            if (!_isAlive) return;

            // [FIX] Xử lý lifetime — auto despawn
            if (lifetime > 0f)
            {
                float age = Time.time - _spawnTime;
                float timeLeft = lifetime - age;

                // Bắt đầu blink khi gần hết hạn
                if (timeLeft <= blinkWarningTime && !_isBlinking)
                {
                    _isBlinking = true;
                }

                // Hết hạn → despawn
                if (timeLeft <= 0f)
                {
                    ReturnToPool();
                    return;
                }
            }

            // Blink effect
            if (_isBlinking && _renderer != null)
            {
                bool visible = Mathf.Sin(Time.time * blinkRate * Mathf.PI * 2f) > 0f;
                _renderer.enabled = visible;
            }

            // [FIX] Không chạy attraction logic khi player đã chết
            if (_playerTransform == null)
            {
                // Thử tìm lại player (có thể đã respawn)
                FindPlayerReference();
                return; // Không chạy attraction — tiết kiệm performance
            }

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

            // [FIX] Đảm bảo renderer bật lại trước khi về pool
            if (_renderer != null) _renderer.enabled = true;

            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        public void OnObjectSpawn()
        {
            _isAlive = true;
            _isAttracted = false;
            _isBlinking = false;
            _spawnTime = Time.time;

            // Reset renderer
            if (_renderer != null) _renderer.enabled = true;

            FindPlayerReference();
        }

        public void OnObjectReturn()
        {
            _playerTransform = null;
            _isBlinking = false;

            if (_renderer != null) _renderer.enabled = true;
        }
    }
}