using UnityEngine;
using EchoMage.Interfaces;
using Unity.Mathematics;
using BillUtils.ObjectPooler;

namespace EchoMage.Combat
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyProjectile : MonoBehaviour, IPoolableObject
    {
        [Header("Behavior")]
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _lifetime = 5f;

        [Header("Collision Layers")]
        [Tooltip("Các layer mà viên đạn này sẽ gây sát thương.")]
        [SerializeField] private LayerMask _damageableLayers;
        [Tooltip("Các layer mà viên đạn sẽ bị phá hủy khi va chạm nhưng không gây sát thương.")]
        [SerializeField] private LayerMask _obstacleLayers;

        private const string ENEMY_PROJECTILE_HIT_ID = "EnemyProjectileHit";
        private float _damage;
        private Coroutine _lifetimeCoroutine;

        public void Initialize(float damage)
        {
            _damage = damage;
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * (_speed * Time.deltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject otherObject = other.gameObject;
            int otherLayer = 1 << otherObject.layer;

            // Kiểm tra xem có phải là layer chướng ngại vật không
            if ((_obstacleLayers.value & otherLayer) > 0)
            {
                ObjectPoolManager.Instance.Spawn(ENEMY_PROJECTILE_HIT_ID, transform.position, quaternion.identity);
                ReturnToPool();
            }

            // Kiểm tra xem có phải là layer gây sát thương không
            if ((_damageableLayers.value & otherLayer) > 0)
            {
                if (otherObject.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(_damage);
                }
                ObjectPoolManager.Instance.Spawn(ENEMY_PROJECTILE_HIT_ID, transform.position, quaternion.identity);
                ReturnToPool();
                return;
            }



            // Nếu không phải cả hai, viên đạn sẽ đi xuyên qua
        }

        private System.Collections.IEnumerator ReturnAfterTime()
        {
            yield return new WaitForSeconds(_lifetime);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        public void OnObjectSpawn()
        {
            _lifetimeCoroutine = StartCoroutine(ReturnAfterTime());
        }

        public void OnObjectReturn()
        {
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }
        }
    }
}