using UnityEngine;
using EchoMage.Combat;
using BillUtils.ObjectPooler;

namespace EchoMage.Enemies
{
    public class RangedEnemy : EnemyBase
    {
        [Header("Ranged Attack Specifics")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _firePoint;

        protected override void Attack()
        {
            if (_projectilePrefab == null || _firePoint == null || PlayerTarget == null)
            {
                _attackCooldownGate.StartCooldown();
                return;
            }

            Vector3 directionToPlayer = PlayerTarget.position - _firePoint.position;
            directionToPlayer.y = 0;
            directionToPlayer.Normalize();

            if (directionToPlayer == Vector3.zero)
            {
                _attackCooldownGate.StartCooldown();
                return;
            }

            Quaternion projectileRotation = Quaternion.LookRotation(directionToPlayer);
            GameObject projectileInstance = ObjectPoolManager.Instance.Spawn(_projectilePrefab, _firePoint.position, projectileRotation);

            if (projectileInstance.TryGetComponent<EnemyProjectile>(out var enemyProjectile))
            {
                enemyProjectile.Initialize(_baseStats.Damage * _threatMultiplier);
            }

            _attackCooldownGate.StartCooldown();
        }
    }
}