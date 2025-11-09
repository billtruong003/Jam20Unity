using UnityEngine;
using EchoMage.Player;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace EchoMage.Loot
{
    [RequireComponent(typeof(Collider))]
    public class UpgradePickup : MonoBehaviour
    {
        [SerializeField] private StatUpgradeData upgradeData;
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private float attractionRadius = 5f;
        [SerializeField] private float attractionSpeed = 8f;

        private Transform _playerTransform;
        private bool _isAttracted = false;

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            {
                _playerTransform = GameManager.Instance.PlayerTransform;
            }
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (distanceToPlayer <= attractionRadius)
            {
                _isAttracted = true;
            }

            if (_isAttracted)
            {
                transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, attractionSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (other.TryGetComponent<PlayerStats>(out var playerStats))
            {
                playerStats.ApplyUpgrade(upgradeData);
                TriggerEffects();
                Destroy(gameObject);
            }
        }

        private void TriggerEffects()
        {
            if (pickupEffectPrefab != null && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Spawn(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            if (pickupSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(pickupSound);
            }
        }
    }
}