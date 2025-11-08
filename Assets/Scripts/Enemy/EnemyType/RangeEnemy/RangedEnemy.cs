using UnityEngine;
using EchoMage.Combat;

namespace EchoMage.Enemies
{
    public class RangedEnemy : EnemyBase
    {
        [Header("Ranged Attack Specifics")]
        [Tooltip("Prefab của viên đạn sẽ được bắn ra.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Vị trí viên đạn được sinh ra.")]
        [SerializeField] private Transform _firePoint;

        protected override void Attack()
        {
            if (_projectilePrefab == null || _firePoint == null || PlayerTarget == null)
            {
                // Đảm bảo không có lỗi nếu thiếu thiết lập
                _attackCooldownGate.StartCooldown();
                return;
            }

            // Tính toán hướng bắn tới người chơi
            Vector3 directionToPlayer = (PlayerTarget.position - _firePoint.position).normalized;
            directionToPlayer.y = 0;

            directionToPlayer.Normalize();

            if (directionToPlayer == Vector3.zero)
            {
                _attackCooldownGate.StartCooldown();
                return;
            }

            Quaternion projectileRotation = Quaternion.LookRotation(directionToPlayer);

            // Lấy viên đạn từ Object Pool
            GameObject projectileInstance = ObjectPoolManager.Instance.Spawn(_projectilePrefab, _firePoint.position, projectileRotation);

            // Khởi tạo chỉ số cho viên đạn
            if (projectileInstance.TryGetComponent<EnemyProjectile>(out var enemyProjectile))
            {
                enemyProjectile.Initialize(_baseStats.Damage * _threatMultiplier);
            }

            _attackCooldownGate.StartCooldown();
        }
    }
}