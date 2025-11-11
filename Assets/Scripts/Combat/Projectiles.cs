using UnityEngine;
using EchoMage.Interfaces;
using EchoMage.Player;
using Shmackle.Utils.CoroutinesTimer;
using System.Collections;
using Unity.Mathematics;
using BillUtils.ObjectPooler;
using EchoMage.Core;

namespace EchoMage.Combat
{
    [AddComponentMenu("EchoMage/Combat/Projectile")]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask projLayer;
        [SerializeField] private AudioClip audioClip;
        private const string PROJECTILE_HIT_ID = "PlayerProjectileHit";
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
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }
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

        int layer;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("IgnoreProjectile")) return;

            layer = other.gameObject.layer;
            // If the object's layer is included in the projLayer mask (friendly/projectile layer)
            if ((projLayer.value & (1 << layer)) != 0)
            {
                ObjectPoolManager.Instance.Spawn(PROJECTILE_HIT_ID, transform.position, Quaternion.identity);
                SoundManager.Instance.PlaySfx(audioClip, this.transform.position);
                ReturnToPool();
                return;
            }

            // If the object's layer is included in the enemyLayer mask
            if ((enemyLayer.value & (1 << layer)) != 0)
            {
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(_damage);
                    SoundManager.Instance.PlaySfx(audioClip, this.transform.position);
                    ObjectPoolManager.Instance.Spawn(PROJECTILE_HIT_ID, transform.position, Quaternion.identity);
                    HandlePierce();
                }
                // Không return → rơi xuống default để destroy
            }

            // DEFAULT: Hit walls/environment/other → Destroy luôn
            ObjectPoolManager.Instance.Spawn(PROJECTILE_HIT_ID, transform.position, Quaternion.identity);
            ReturnToPool();
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
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        private void TrailControl(bool trailState)
        {
            trailRenderer.enabled = trailState;
            if (!trailState) trailRenderer.Clear();
        }
    }
}