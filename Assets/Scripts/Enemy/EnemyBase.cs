using UnityEngine;
using UnityEngine.AI;
using EchoMage.Interfaces;
using Utilities.Timers;
using EchoMage.Player;
using EchoMage.Core;

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

        private DespairSystem _despairSystem;

        protected virtual void Awake()
        {
            NavAgent = GetComponent<NavMeshAgent>();
            VatAnimator = GetComponent<VAT_Animator>();
            _despairSystem = GameManager.Instance.UIManager.GetDespairSystem;
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
                if (_attackCooldownGate.IsReady)
                {
                    SwitchState(State.Attacking);
                }
                else
                {
                    SwitchState(State.Idle);
                }
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
                    NavAgent.enabled = false;
                    GetComponent<Collider>().enabled = false;
                    VatAnimator.CrossFade(_baseStats.DeathClipName, 0.1f);
                    Invoke(nameof(ReturnToPool), 1.5f);
                    break;
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
                    transform.rotation = Quaternion.LookRotation(lookDirection);
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
                _despairSystem?.UnregisterEnemy(gameObject);
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

            _despairSystem?.RegisterEnemy(gameObject);

            SwitchState(State.Chasing);
        }

        public void OnObjectReturn()
        {
            PlayerTarget = null;
            NavAgent.enabled = false;

            // Đảm bảo hủy đăng ký trong mọi trường hợp, kể cả khi bị thu hồi mà không chết
            _despairSystem?.UnregisterEnemy(gameObject);
        }
    }
}