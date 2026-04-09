using UnityEngine;
using UnityEngine.AI;
using EchoMage.Interfaces;
using Utilities.Timers;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace EchoMage.Enemies
{
    [RequireComponent(typeof(NavMeshAgent), typeof(VAT_Animator))]
    public abstract class EnemyBase : MonoBehaviour, IPoolableObject, IDamageable
    {
        [SerializeField] protected EnemyStats _baseStats;

        protected NavMeshAgent NavAgent;
        protected VAT_Animator VatAnimator;
        protected Transform PlayerTarget;

        protected float _currentHealth;
        protected float _threatMultiplier = 1f;
        // [MỚI] Separate multipliers cho damage và speed
        protected float _damageMultiplier = 1f;
        protected float _speedMultiplier = 1f;
        protected TimeGate _attackCooldownGate;

        private enum State { Spawning, Idle, Chasing, Attacking, Stunned, Dead }
        private State _currentState;

        protected virtual void Awake()
        {
            NavAgent = GetComponent<NavMeshAgent>();
            VatAnimator = GetComponent<VAT_Animator>();
        }

        /// <summary>
        /// [MỚI] Initialize với separate multipliers từ DifficultyManager.
        /// HP, Damage, Speed đều scale độc lập theo curve riêng.
        /// </summary>
        public void InitializeWithDifficulty(Transform target, float healthMult, float damageMult, float speedMult)
        {
            PlayerTarget = target;
            _threatMultiplier = healthMult;
            _damageMultiplier = damageMult;
            _speedMultiplier = speedMult;

            _currentHealth = _baseStats.MaxHealth * healthMult;
            NavAgent.speed = _baseStats.MoveSpeed * speedMult;

            _attackCooldownGate = new TimeGate(_baseStats.AttackCooldown);
        }

        /// <summary>
        /// [Backward compat] Initialize cũ — dùng chung 1 multiplier cho tất cả.
        /// BossEnemy và code cũ vẫn dùng được.
        /// </summary>
        public void Initialize(Transform target, float threatLevel)
        {
            InitializeWithDifficulty(target, threatLevel, threatLevel, 1f);
        }

        private void Update()
        {
            if (PlayerTarget == null || _currentState == State.Dead || _currentState == State.Stunned) return;

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTarget.position);
            bool isPlayerInRange = distanceToPlayer <= _baseStats.AttackRange;

            if (isPlayerInRange)
            {
                SwitchState(_attackCooldownGate.IsReady ? State.Attacking : State.Idle);
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
                    NavAgent.isStopped = true;
                    VatAnimator.CrossFade(_baseStats.IdleClipName, 0.2f);
                    break;
                case State.Chasing:
                    NavAgent.isStopped = false;
                    VatAnimator.CrossFade(_baseStats.MoveClipName, 0.2f);
                    break;
                case State.Attacking:
                    NavAgent.isStopped = true;
                    Attack();
                    break;
                case State.Dead:
                    HandleDeath();
                    break;
            }
        }

        private void HandleDeath()
        {
            GameManager.Instance.UnregisterEnemy(gameObject);
            GameSessionManager.Instance.AddScore(_baseStats.ScoreValue);
            HandleLootDrop();
            NavAgent.enabled = false;
            GetComponent<Collider>().enabled = false;
            VatAnimator.CrossFade(_baseStats.DeathClipName, 0.1f);
            Invoke(nameof(ReturnToPool), 1.5f);
        }

        public void ForceKill()
        {
            if (_currentState == State.Dead) return;
            _currentState = State.Dead;

            GameManager.Instance.UnregisterEnemy(gameObject);
            GameSessionManager.Instance.AddScore(_baseStats.ScoreValue);
            NavAgent.enabled = false;
            GetComponent<Collider>().enabled = false;
            ReturnToPool();
        }

        private void HandleLootDrop()
        {
            if (_baseStats.LootTable == null) return;

            foreach (var drop in _baseStats.LootTable.PotentialDrops)
            {
                if (Random.value <= drop.DropChance)
                {
                    ObjectPoolManager.Instance.Spawn(drop.ItemPrefab, transform.position, Quaternion.identity);
                    return;
                }
            }
        }

        private void HandleCurrentState()
        {
            switch (_currentState)
            {
                case State.Chasing:
                    NavAgent.SetDestination(PlayerTarget.position);
                    break;
                case State.Idle:
                case State.Attacking:
                    Vector3 lookDirection = PlayerTarget.position - transform.position;
                    lookDirection.y = 0;
                    if (lookDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(lookDirection);
                    }
                    break;
            }
        }

        public void TakeDamage(float amount)
        {
            if (_currentState == State.Dead) return;

            _currentHealth -= amount;
            VatAnimator.Play(_baseStats.HitClipName);

            if (_currentHealth <= 0)
            {
                SwitchState(State.Dead);
            }
        }

        /// <summary>
        /// Lấy damage thực tế đã nhân difficulty multiplier.
        /// Các subclass (MeleeEnemy, SpeedEnemy) dùng cái này thay vì _baseStats.Damage trực tiếp.
        /// </summary>
        protected float GetScaledDamage()
        {
            return _baseStats.Damage * _damageMultiplier;
        }

        protected abstract void Attack();

        /// <summary>
        /// [FIX] Cập nhật target khi player respawn.
        /// GameManager gọi hàm này cho tất cả enemy đang sống sau khi player hồi sinh.
        /// </summary>
        public void UpdatePlayerTarget(Transform newTarget)
        {
            PlayerTarget = newTarget;
        }

        private void ReturnToPool()
        {
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        public virtual void OnObjectSpawn()
        {
            _currentState = State.Spawning;
            NavAgent.enabled = true;
            NavAgent.isStopped = true;
            GetComponent<Collider>().enabled = true;
            GameManager.Instance.RegisterEnemy(gameObject);
            SwitchState(State.Chasing);
        }

        public virtual void OnObjectReturn()
        {
            if (_currentState != State.Dead)
            {
                GameManager.Instance.UnregisterEnemy(gameObject);
            }
            PlayerTarget = null;
            NavAgent.enabled = false;
        }
    }
}