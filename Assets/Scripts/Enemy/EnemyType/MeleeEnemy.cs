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

        [Tooltip("Vị trí của trung tâm vùng tấn công, tính từ vị trí của kẻ địch theo hướng tấn công.")]
        [SerializeField] private float _attackOffset = 1.0f;

        private static readonly Collider[] _hitColliders = new Collider[1]; // Tối ưu hóa: Tránh cấp phát bộ nhớ mỗi lần gọi

        protected override void Attack()
        {
            if (PlayerTarget == null) return;

            // Xác định trung tâm của vùng tấn công phía trước mặt kẻ địch
            Vector3 attackCenter = transform.position + transform.forward * _attackOffset;

            int hits = Physics.OverlapSphereNonAlloc(attackCenter, _attackRadius, _hitColliders, _playerLayerMask);

            if (hits > 0)
            {
                // Vì chỉ có một người chơi, chúng ta chỉ cần xử lý collider đầu tiên
                if (_hitColliders[0].TryGetComponent<IDamageable>(out var playerDamageable))
                {
                    playerDamageable.TakeDamage(_baseStats.Damage * _threatMultiplier);
                }
            }

            // Đặt lại cooldown ngay sau khi thực hiện logic tấn công
            _attackCooldownGate.StartCooldown();
        }

        // Tùy chọn: Vẽ Gizmo trong Editor để dễ dàng hình dung vùng tấn công
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 attackCenter = transform.position + transform.forward * _attackOffset;
            Gizmos.DrawWireSphere(attackCenter, _attackRadius);
        }
    }
}