using UnityEngine;
using EchoMage.Interfaces;
using EchoMage.Player;
using Shmackle.Utils.CoroutinesTimer;
using System.Collections;

namespace EchoMage.Combat
{
    [AddComponentMenu("EchoMage/Combat/Projectile")]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private TrailRenderer trailRenderer;
        private float _damage;
        private float _speed;
        private int _maxPierceCount;
        private float _lifetime;
        private int _currentPierce;
        private Coroutine _lifetimeCoroutine;

        public struct ProjectileData
        {
            public float Damage;
            public float Speed;
            public int PierceCount;
            public float Lifetime;
            public float Scale;
            // You could add more here later, like a "DamageType" enum or "StatusEffect"
        }

        public void Initialize(PlayerStats stats)
        {
            _damage = stats.Damage;
            _speed = stats.ProjectileSpeed;
            _maxPierceCount = stats.PierceCount;
            _lifetime = stats.ProjectileLifetime;
            transform.localScale = Vector3.one * stats.ProjectileScale;
        }

        public void OnObjectSpawn()
        {
            _currentPierce = _maxPierceCount;
            _lifetimeCoroutine = StartCoroutine(ReturnToPoolAfterTime(_lifetime));
            TrailControl(true);
        }

        public void OnObjectReturn()
        {
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * _speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("IgnoreProjectile")) return;

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                HandlePierce();
            }
            else
            {
                ReturnToPool();
            }
        }

        private void HandlePierce()
        {
            _currentPierce--;
            if (_currentPierce <= 0)
            {
                ReturnToPool();
            }
        }

        private IEnumerator ReturnToPoolAfterTime(float delay)
        {
            yield return CoroutineTimeUtils.GetWaitForSeconds(delay);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            TrailControl(false);
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }

        private void TrailControl(bool trailState)
        {
            trailRenderer.enabled = trailState;
            if (!trailState) trailRenderer.Clear();
        }
    }
}