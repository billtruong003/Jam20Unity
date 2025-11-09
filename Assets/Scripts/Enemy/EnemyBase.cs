using UnityEngine;
using UnityEngine.AI;
using EchoMage.Interfaces;
using Utilities.Timers;
using EchoMage.Core;
using EchoMage.Loot; // Thêm namespace

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
        protected TimeGate _attackCooldownGate;

        private enum State { Spawning, Idle, Chasing, Attacking, Stunned, Dead }
        private State _currentState;

        protected virtual void Awake()
        {
            NavAgent = GetComponent<NavMeshAgent>();
            VatAnimator = GetComponent<VAT_Animator>();
        }

        public void Initialize(Transform target, float threatLevel)
        {
            PlayerTarget = target;
            _threatMultiplier = threatLevel;

            _currentHealth = _baseStats.MaxHealth * _threatMultiplier;
            NavAgent.speed = _baseStats.MoveSpeed;

            _attackCooldownGate = new TimeGate(_baseStats.AttackCooldown);
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
                    HandleDeath(); // Thay thế logic cũ bằng một hàm riêng
                    break;
            }
        }

        private void HandleDeath()
        {
            GameManager.Instance.UnregisterEnemy(gameObject);
            HandleLootDrop(); // Gọi hàm rơi đồ
            NavAgent.enabled = false;
            GetComponent<Collider>().enabled = false;
            VatAnimator.CrossFade(_baseStats.DeathClipName, 0.1f);
            Invoke(nameof(ReturnToPool), 1.5f);
        }

        private void HandleLootDrop()
        {
            if (_baseStats.LootTable == null) return;

            foreach (var drop in _baseStats.LootTable.PotentialDrops)
            {
                if (Random.value <= drop.DropChance)
                {
                    Instantiate(drop.ItemPrefab, transform.position, Quaternion.identity);
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

        protected abstract void Attack();

        private void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }

        public void OnObjectSpawn()
        {
            _currentState = State.Spawning;
            NavAgent.enabled = true;
            NavAgent.isStopped = true;
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
            PlayerTarget = null;
            NavAgent.enabled = false;
        }
    }
}