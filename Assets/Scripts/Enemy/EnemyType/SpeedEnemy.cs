using System.Collections;
using UnityEngine;
using EchoMage.Interfaces;

namespace EchoMage.Enemies
{
    public class SpeedEnemy : EnemyBase
    {
        [Header("Dash Attack Specifics")]
        [Tooltip("Tốc độ của cú lao tới.")]
        [SerializeField] private float _dashSpeed = 25f;

        [Tooltip("Khoảng thời gian kẻ địch chuẩn bị trước khi lao.")]
        [SerializeField] private float _telegraphDuration = 0.5f;

        [Tooltip("Layer của người chơi để kiểm tra va chạm khi đang lao.")]
        [SerializeField] private LayerMask _playerLayerMask;

        private bool _isDashing = false;
        private Coroutine _dashCoroutine;

        protected override void Attack()
        {
            if (_isDashing || PlayerTarget == null) return;

            _dashCoroutine = StartCoroutine(DashAttackRoutine());
            _attackCooldownGate.StartCooldown();
        }

        private IEnumerator DashAttackRoutine()
        {
            _isDashing = true;
            NavAgent.enabled = false; // Tắt NavMeshAgent để điều khiển thủ công

            // Giai đoạn chuẩn bị (Telegraph)
            // Có thể thêm hiệu ứng hình ảnh/âm thanh ở đây để cảnh báo người chơi
            yield return new WaitForSeconds(_telegraphDuration);

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = PlayerTarget.position; // Chốt vị trí của người chơi tại thời điểm lao tới
            float distanceToTarget = Vector3.Distance(startPosition, targetPosition);
            float dashDuration = distanceToTarget / _dashSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < dashDuration)
            {
                float interpolationRatio = elapsedTime / dashDuration;
                transform.position = Vector3.Lerp(startPosition, targetPosition, interpolationRatio);

                // Kiểm tra va chạm với người chơi trong khi đang lao
                if (Physics.CheckSphere(transform.position, 1.0f, _playerLayerMask))
                {
                    if (PlayerTarget.TryGetComponent<IDamageable>(out var playerDamageable))
                    {
                        playerDamageable.TakeDamage(_baseStats.Damage * _threatMultiplier);
                    }
                    // Đã gây sát thương, kết thúc cú lao
                    break;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            NavAgent.enabled = true; // Bật lại NavMeshAgent
            _isDashing = false;
        }

        // Đảm bảo coroutine được dừng khi object bị trả về pool
        public new void OnObjectReturn()
        {
            base.OnObjectReturn();
            if (_dashCoroutine != null)
            {
                StopCoroutine(_dashCoroutine);
                _dashCoroutine = null;
            }
            _isDashing = false;
        }
    }
}