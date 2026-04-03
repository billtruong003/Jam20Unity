using System.Collections;
using UnityEngine;
using EchoMage.Interfaces;

namespace EchoMage.Enemies
{
    public class SpeedEnemy : EnemyBase
    {
        [Header("Dash Attack Specifics")]
        [SerializeField] private float _dashSpeed = 25f;
        [SerializeField] private float _telegraphDuration = 0.5f;
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
            NavAgent.enabled = false;

            yield return new WaitForSeconds(_telegraphDuration);

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = PlayerTarget.position;
            float distanceToTarget = Vector3.Distance(startPosition, targetPosition);
            float dashDuration = distanceToTarget / _dashSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < dashDuration)
            {
                float interpolationRatio = elapsedTime / dashDuration;
                transform.position = Vector3.Lerp(startPosition, targetPosition, interpolationRatio);

                if (Physics.CheckSphere(transform.position, 1.0f, _playerLayerMask))
                {
                    if (PlayerTarget.TryGetComponent<IDamageable>(out var playerDamageable))
                    {
                        // [FIX] Dùng GetScaledDamage() — damage scale theo DifficultyManager curve
                        playerDamageable.TakeDamage(GetScaledDamage());
                    }
                    break;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            NavAgent.enabled = true;
            _isDashing = false;
        }

        public override void OnObjectReturn()
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