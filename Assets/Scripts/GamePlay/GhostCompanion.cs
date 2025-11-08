// Assets/Scripts/AI/GhostCompanion.cs

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

        public void Initialize(EchoData data)
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
            _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
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
            else // Attacking
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
                transform.rotation = Quaternion.LookRotation(direction);
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

            Vector3 directionToTarget = (_currentTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget), Time.deltaTime * 10f);

            if (_attackGate.TryPass())
            {
                _vatAnimator.Play(attackClip);
                FireProjectile();
            }
        }

        private void FireProjectile()
        {
            GameObject projectileObject = ObjectPoolManager.Instance.Spawn("PlayerProjectile", transform.position, transform.rotation);
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