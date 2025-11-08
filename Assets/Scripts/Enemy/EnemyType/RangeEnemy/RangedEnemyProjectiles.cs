using UnityEngine;
using EchoMage.Interfaces;

namespace EchoMage.Combat
{
    [RequireComponent(typeof(Collider))]
    public class EnemyProjectile : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _lifetime = 5f;

        private float _damage;
        private Coroutine _lifetimeCoroutine;

        public void Initialize(float damage)
        {
            _damage = damage;
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * _speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy")) return; // Bỏ qua va chạm với kẻ địch khác

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
            }

            // Dù trúng hay không, viên đạn sẽ biến mất khi va chạm
            ReturnToPool();
        }

        private System.Collections.IEnumerator ReturnAfterTime()
        {
            yield return new WaitForSeconds(_lifetime);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
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