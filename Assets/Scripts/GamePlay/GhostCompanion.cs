using EchoMage.Core;
using EchoMage.Echoes;
using EchoMage.Player;
using UnityEngine;
using Utilities.Timers;

namespace EchoMage.AI
{
    [RequireComponent(typeof(PlayerStats), typeof(VAT_Animator))]
    public class GhostCompanion : MonoBehaviour
    {
        [Header("Behavior")]
        [SerializeField] private float followDistance = 4f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float attackRange = 15f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Animation Names")]
        [SerializeField] private string idleClip = "Idle";
        [SerializeField] private string moveClip = "Move";
        [SerializeField] private string attackClip = "Attack";

        private PlayerStats _stats;
        private VAT_Animator _vatAnimator;
        private Transform _playerTransform;
        private Transform _currentTarget;
        private TimeGate _attackGate;

        private enum State { Following, Attacking }
        private State _currentState;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _vatAnimator = GetComponent<VAT_Animator>();
        }

        // SỬA LỖI: Thay đổi kiểu dữ liệu từ EchoData sang PlayerEchoData
        public void Initialize(PlayerEchoData data)
        {
            _stats.Damage = data.Damage;
            _stats.AttackCooldown = data.AttackCooldown;
            _stats.ProjectilesPerShot = data.ProjectilesPerShot;
            _stats.ProjectileSpeed = data.ProjectileSpeed;
            _stats.PierceCount = data.PierceCount;
            _stats.ProjectileScale = data.ProjectileScale;
            _stats.ProjectileLifetime = data.ProjectileLifetime;
            _stats.ProjectileSpreadAngle = data.ProjectileSpreadAngle;

            _attackGate = new TimeGate(_stats.AttackCooldown);

            // Lấy tham chiếu đến người chơi một cách an toàn
            if (GameManager.Instance != null)
            {
                _playerTransform = GameManager.Instance.PlayerTransform;
            }
        }

        private void Update()
        {
            if (_playerTransform == null) return;
            FindTarget();
            UpdateState();
            ExecuteStateAction();
        }

        private void FindTarget()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            _currentTarget = FindClosestEnemy(enemies);
        }

        private void UpdateState()
        {
            State newState = (_currentTarget != null) ? State.Attacking : State.Following;
            if (newState != _currentState)
            {
                _currentState = newState;
                string animClip = (_currentState == State.Following) ? moveClip : idleClip;
                _vatAnimator.CrossFade(animClip, 0.2f);
            }
        }

        private void ExecuteStateAction()
        {
            if (_currentState == State.Following)
            {
                FollowPlayer();
            }
            else
            {
                AttackTarget();
            }
        }

        private void FollowPlayer()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            if (distanceToPlayer > followDistance)
            {
                Vector3 direction = (_playerTransform.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
                _vatAnimator.CrossFade(moveClip, 0.2f);
            }
            else
            {
                _vatAnimator.CrossFade(idleClip, 0.2f);
            }
        }

        private void AttackTarget()
        {
            if (_currentTarget == null) return;

            Vector3 directionToTarget = _currentTarget.position - transform.position;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget), Time.deltaTime * 10f);
            }

            if (_attackGate.TryPass())
            {
                _vatAnimator.Play(attackClip);
                FireProjectiles();
            }
        }

        private void FireProjectiles()
        {
            Vector3 firePosition = transform.position;
            Vector3 targetPosition = _currentTarget.position;

            Vector3 baseDirection = targetPosition - firePosition;
            baseDirection.y = 0;
            baseDirection.Normalize();

            if (baseDirection == Vector3.zero) return;

            int count = _stats.ProjectilesPerShot;
            if (count <= 1)
            {
                SpawnSingleProjectile(firePosition, Quaternion.LookRotation(baseDirection));
                return;
            }

            float totalAngle = _stats.ProjectileSpreadAngle;
            float angleStep = totalAngle / (count - 1);
            float startAngle = -totalAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float currentAngle = startAngle + (i * angleStep);
                Quaternion rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.up);
                Vector3 finalDirection = rotationOffset * baseDirection;
                SpawnSingleProjectile(firePosition, Quaternion.LookRotation(finalDirection));
            }
        }

        private void SpawnSingleProjectile(Vector3 position, Quaternion rotation)
        {
            GameObject projectileObject = ObjectPoolManager.Instance.Spawn("PlayerProjectile", position, rotation);
            if (projectileObject != null && projectileObject.TryGetComponent<Combat.Projectile>(out var projectile))
            {
                projectile.Initialize(_stats);
            }
        }

        private Transform FindClosestEnemy(Collider[] enemies)
        {
            Transform bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;
            foreach (var potentialTarget in enemies)
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget.transform;
                }
            }
            return bestTarget;
        }
    }
}