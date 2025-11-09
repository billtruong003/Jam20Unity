using UnityEngine;
using EchoMage.Interfaces;

namespace EchoMage.Enemies.Types
{
    public class MeleeEnemy : EnemyBase
    {
        [Header("Melee Attack Specifics")]
        [Tooltip("Layer của người chơi để giới hạn việc kiểm tra va chạm.")]
        [SerializeField] private LayerMask _playerLayerMask;

        [Tooltip("Bán kính của vùng tấn công.")]
        [SerializeField] private float _attackRadius = 1.2f;

        [Tooltip("Vị trí của trung tâm vùng tấn công, tính từ vị trí của kẻ địch.")]
        [SerializeField] private Vector3 _attackOffset = new Vector3(0, 0, 1.0f);

        private static readonly Collider[] _hitColliders = new Collider[1];
        private const string ATTACK_HIT_FX = "MeleeEnemyHit";

        protected override void Attack()
        {
            if (PlayerTarget == null) return;

            Vector3 attackCenter = transform.TransformPoint(_attackOffset);

            int hits = Physics.OverlapSphereNonAlloc(attackCenter, _attackRadius, _hitColliders, _playerLayerMask);

            if (hits > 0 && _hitColliders[0].TryGetComponent<IDamageable>(out var playerDamageable))
            {
                playerDamageable.TakeDamage(_baseStats.Damage * _threatMultiplier);
                ObjectPoolManager.Instance.Spawn("MeleeEnemyHit", _hitColliders[0].transform.position, Quaternion.identity);
            }

            _attackCooldownGate.StartCooldown();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 attackCenter = transform.TransformPoint(_attackOffset);
            Gizmos.DrawWireSphere(attackCenter, _attackRadius);
        }
    }
}