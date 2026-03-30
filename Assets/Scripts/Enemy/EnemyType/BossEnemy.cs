using System;
using UnityEngine;
using UnityEngine.AI;
using EchoMage.Interfaces;
using Utilities.Timers;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace EchoMage.Enemies
{
    /// <summary>
    /// Boss enemy - xuất hiện sau 10 phút chơi.
    /// Khi chết sẽ gây nổ giết tất cả quái xung quanh + thưởng điểm lớn.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(VAT_Animator))]
    public class BossEnemy : MonoBehaviour, IPoolableObject, IDamageable
    {
        /// <summary>
        /// Event khi Boss chết - EnemySpawner lắng nghe để xử lý nổ + respawn.
        /// </summary>
        public event Action<BossEnemy> OnBossDeath;

        /// <summary>
        /// Event cập nhật HP cho UI hiển thị thanh máu Boss.
        /// </summary>
        public event Action<float, float> OnBossHealthChanged;

        [Header("Boss Stats")]
        [SerializeField] private float _maxHealth = 2000f;
        [SerializeField] private float _damage = 30f;
        [SerializeField] private float _moveSpeed = 2.5f;
        [SerializeField] private float _attackRange = 3f;
        [SerializeField] private float _attackCooldown = 1.5f;

        [Header("Boss Attack")]
        [Tooltip("Bán kính tấn công AOE của Boss.")]
        [SerializeField] private float _attackAOERadius = 4f;
        [SerializeField] private LayerMask _playerLayerMask;

        [Header("VAT Animation Clips")]
        [SerializeField] private string _idleClip = "Idle";
        [SerializeField] private string _moveClip = "Move";
        [SerializeField] private string _attackClip = "Attack";
        [SerializeField] private string _hitClip = "Hit";
        [SerializeField] private string _deathClip = "Die";

        [Header("Visual Effects")]
        [SerializeField] private string _deathExplosionFXId = "BossDeathExplosion";

        private NavMeshAgent _navAgent;
        private VAT_Animator _vatAnimator;
        private Transform _playerTarget;
        private float _currentHealth;
        private float _threatMultiplier = 1f;
        private TimeGate _attackGate;

        private enum State { Idle, Chasing, Attacking, Dead }
        private State _currentState;

        private static readonly Collider[] _hitColliders = new Collider[4];

        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            _vatAnimator = GetComponent<VAT_Animator>();
        }

        public void Initialize(Transform target, float threatLevel)
        {
            _playerTarget = target;
            _threatMultiplier = threatLevel;
            _currentHealth = _maxHealth * _threatMultiplier;
            _navAgent.speed = _moveSpeed;
            _attackGate = new TimeGate(_attackCooldown);

            // Broadcast HP ban đầu
            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth * _threatMultiplier);
        }

        private void Update()
        {
            if (_playerTarget == null || _currentState == State.Dead) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTarget.position);
            bool isPlayerInRange = distanceToPlayer <= _attackRange;

            if (isPlayerInRange)
            {
                SwitchState(_attackGate.IsReady ? State.Attacking : State.Idle);
            }
            else
            {
                SwitchState(State.Chasing);
            }

            HandleCurrentState();
        }

        private void SwitchState(State newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;

            switch (_currentState)
            {
                case State.Idle:
                    _navAgent.isStopped = true;
                    _vatAnimator.CrossFade(_idleClip, 0.2f);
                    break;
                case State.Chasing:
                    _navAgent.isStopped = false;
                    _vatAnimator.CrossFade(_moveClip, 0.2f);
                    break;
                case State.Attacking:
                    _navAgent.isStopped = true;
                    PerformAttack();
                    break;
                case State.Dead:
                    HandleDeath();
                    break;
            }
        }

        private void HandleCurrentState()
        {
            switch (_currentState)
            {
                case State.Chasing:
                    _navAgent.SetDestination(_playerTarget.position);
                    break;
                case State.Idle:
                case State.Attacking:
                    Vector3 lookDirection = _playerTarget.position - transform.position;
                    lookDirection.y = 0;
                    if (lookDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(lookDirection);
                    }
                    break;
            }
        }

        private void PerformAttack()
        {
            _vatAnimator.Play(_attackClip);

            // Boss tấn công AOE
            Vector3 attackCenter = transform.position + transform.forward * (_attackRange * 0.5f);
            int hits = Physics.OverlapSphereNonAlloc(attackCenter, _attackAOERadius, _hitColliders, _playerLayerMask);

            for (int i = 0; i < hits; i++)
            {
                if (_hitColliders[i].TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(_damage * _threatMultiplier);
                }
            }

            _attackGate.StartCooldown();
        }

        public void TakeDamage(float amount)
        {
            if (_currentState == State.Dead) return;

            _currentHealth -= amount;
            _vatAnimator.Play(_hitClip);

            // Cập nhật UI thanh máu Boss
            OnBossHealthChanged?.Invoke(
                Mathf.Max(0, _currentHealth),
                _maxHealth * _threatMultiplier
            );

            if (_currentHealth <= 0)
            {
                SwitchState(State.Dead);
            }
        }

        private void HandleDeath()
        {
            _navAgent.enabled = false;
            GetComponent<Collider>().enabled = false;
            _vatAnimator.CrossFade(_deathClip, 0.1f);

            // Spawn hiệu ứng nổ Boss
            ObjectPoolManager.Instance.Spawn(_deathExplosionFXId, transform.position, Quaternion.identity);

            // Thông báo cho EnemySpawner
            OnBossDeath?.Invoke(this);

            // Trả về pool sau animation chết
            Invoke(nameof(ReturnToPool), 2.5f);
        }

        private void ReturnToPool()
        {
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        #region IPoolableObject

        public void OnObjectSpawn()
        {
            _currentState = State.Idle;
            _navAgent.enabled = true;
            _navAgent.isStopped = true;
            GetComponent<Collider>().enabled = true;
            GameManager.Instance.RegisterEnemy(gameObject);
            SwitchState(State.Chasing);
        }

        public void OnObjectReturn()
        {
            if (_currentState != State.Dead)
            {
                GameManager.Instance.UnregisterEnemy(gameObject);
            }
            _playerTarget = null;
            _navAgent.enabled = false;
            OnBossDeath = null;
            OnBossHealthChanged = null;
        }

        #endregion

        /// <summary>
        /// Lấy % HP hiện tại của Boss (0-1).
        /// </summary>
        public float HealthPercent => _currentHealth / (_maxHealth * _threatMultiplier);
    }
}