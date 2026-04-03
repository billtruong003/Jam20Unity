using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using EchoMage.Interfaces;
using Utilities.Timers;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace EchoMage.Enemies
{
    // ============================================================
    //  DATA STRUCTURES — Cấu hình combo trong Inspector
    // ============================================================

    /// <summary>
    /// Một đòn đánh đơn lẻ trong combo chain.
    /// </summary>
    [Serializable]
    public class BossAttackMove
    {
        [Tooltip("Tên VAT animation clip cho đòn này (vd: Attack_Punch, Attack_Slam).")]
        public string AnimClip = "Attack";

        [Tooltip("Damage của đòn này (nhân với threat multiplier).")]
        public float Damage = 30f;

        [Tooltip("Bán kính vùng gây sát thương.")]
        public float HitRadius = 3f;

        [Tooltip("Offset vị trí hitbox so với boss (hướng forward).")]
        public float HitForwardOffset = 1.5f;

        [Tooltip("Thời gian chờ TRƯỚC khi đòn này gây damage (để sync animation).")]
        public float WindupTime = 0.3f;

        [Tooltip("Thời gian chờ SAU khi gây damage trước đòn tiếp theo.")]
        public float RecoveryTime = 0.4f;

        [Tooltip("ID VFX spawn khi đánh trúng (từ ObjectPool). Để trống nếu không có.")]
        public string HitVFXId = "";

        [Tooltip("Boss di chuyển về phía player bao nhiêu unit trong lúc windup (dash lunge).")]
        public float LungeDistance = 0f;
    }

    /// <summary>
    /// Một combo = chuỗi các đòn đánh liên tiếp.
    /// </summary>
    [Serializable]
    public class BossCombo
    {
        [Tooltip("Tên combo (để debug/UI).")]
        public string ComboName = "Basic Combo";

        [Tooltip("Danh sách các đòn đánh theo thứ tự.")]
        public List<BossAttackMove> Moves = new List<BossAttackMove>();

        [Tooltip("Cooldown sau khi combo kết thúc trước khi có thể combo tiếp.")]
        public float CooldownAfterCombo = 2f;

        [Tooltip("Tỉ lệ được chọn (weight) — cao hơn = hay dùng hơn. Dùng cho random weighted.")]
        [Range(1, 10)]
        public int SelectionWeight = 5;
    }

    /// <summary>
    /// Một phase = giai đoạn HP. Mỗi phase có bộ combo và stats riêng.
    /// </summary>
    [Serializable]
    public class BossPhase
    {
        [Tooltip("Tên phase (vd: Calm, Aggressive, Enraged).")]
        public string PhaseName = "Phase";

        [Tooltip("Phase này kích hoạt khi HP xuống dưới % này (0-1). Phase 1 = 1.0, Phase 2 = 0.7, Phase 3 = 0.4")]
        [Range(0f, 1f)]
        public float HealthThreshold = 1f;

        [Tooltip("Hệ số nhân tốc độ di chuyển trong phase này.")]
        public float MoveSpeedMultiplier = 1f;

        [Tooltip("Hệ số nhân tốc độ animation (nhanh hơn = đánh nhanh hơn).")]
        public float AttackSpeedMultiplier = 1f;

        [Tooltip("Tên VAT clip chuyển phase (roar, transform...). Để trống để skip.")]
        public string PhaseTransitionClip = "";

        [Tooltip("Thời gian khóa boss khi chuyển phase (bất khả xâm phạm).")]
        public float TransitionDuration = 1.5f;

        [Tooltip("VFX khi chuyển phase.")]
        public string TransitionVFXId = "";

        [Tooltip("Danh sách combo dùng được trong phase này.")]
        public List<BossCombo> AvailableCombos = new List<BossCombo>();
    }

    // ============================================================
    //  BOSS ENEMY — Main Controller
    // ============================================================

    [RequireComponent(typeof(NavMeshAgent), typeof(VAT_Animator))]
    public class BossEnemy : MonoBehaviour, IPoolableObject, IDamageable
    {
        public event Action<BossEnemy> OnBossDeath;
        public event Action<float, float> OnBossHealthChanged;
        /// <summary>
        /// Event khi chuyển phase — UI có thể hiện tên phase.
        /// </summary>
        public event Action<int, string> OnPhaseChanged;

        [Header("Base Stats")]
        [SerializeField] private float _maxHealth = 2000f;
        [SerializeField] private float _baseMoveSpeed = 2.5f;
        [SerializeField] private float _chaseRange = 30f;

        [Header("Phase Configuration")]
        [Tooltip("Danh sách các phase theo thứ tự HP giảm dần. Phase đầu tiên phải có threshold = 1.0")]
        [SerializeField] private List<BossPhase> _phases = new List<BossPhase>();

        [Header("Detection")]
        [SerializeField] private float _attackRange = 4f;
        [SerializeField] private LayerMask _playerLayerMask;

        [Header("VAT Base Clips")]
        [SerializeField] private string _idleClip = "Idle";
        [SerializeField] private string _moveClip = "Move";
        [SerializeField] private string _hitClip = "Hit";
        [SerializeField] private string _deathClip = "Die";

        [Header("Visual Effects")]
        [SerializeField] private string _deathExplosionFXId = "BossDeathExplosion";

        // Components
        private NavMeshAgent _navAgent;
        private VAT_Animator _vatAnimator;

        // State
        private Transform _playerTarget;
        private float _currentHealth;
        private float _threatMultiplier = 1f;
        private int _currentPhaseIndex = -1;
        private BossPhase _currentPhase;
        private bool _isPerformingCombo = false;
        private bool _isTransitioning = false;
        private bool _isDead = false;

        private Coroutine _comboCoroutine;
        private TimeGate _comboCooldownGate;
        private static readonly Collider[] _hitResults = new Collider[4];

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
            _isDead = false;
            _isPerformingCombo = false;
            _isTransitioning = false;
            _currentPhaseIndex = -1;

            _navAgent.speed = _baseMoveSpeed;
            _comboCooldownGate = new TimeGate(1f);

            // Khởi tạo phase đầu tiên
            EvaluatePhase();

            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth * _threatMultiplier);
        }

        private void Update()
        {
            if (_isDead || _playerTarget == null || _isTransitioning) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTarget.position);

            if (_isPerformingCombo)
            {
                // Đang đánh combo → quay mặt về player nhưng không di chuyển
                LookAtPlayer();
                return;
            }

            if (distanceToPlayer <= _attackRange && _comboCooldownGate.IsReady)
            {
                // Trong tầm đánh + hết cooldown → bắt đầu combo
                StartRandomCombo();
            }
            else if (distanceToPlayer > _attackRange)
            {
                // Ngoài tầm → chase
                Chase();
            }
            else
            {
                // Trong tầm nhưng đang cooldown → đứng idle
                _navAgent.isStopped = true;
                _vatAnimator.CrossFade(_idleClip, 0.2f);
                LookAtPlayer();
            }
        }

        #region Movement

        private void Chase()
        {
            _navAgent.isStopped = false;
            _navAgent.SetDestination(_playerTarget.position);
            _vatAnimator.CrossFade(_moveClip, 0.2f);
        }

        private void LookAtPlayer()
        {
            Vector3 dir = _playerTarget.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * 10f
                );
            }
        }

        #endregion

        #region Combo System

        private void StartRandomCombo()
        {
            if (_currentPhase == null || _currentPhase.AvailableCombos.Count == 0) return;

            // Weighted random selection
            BossCombo selectedCombo = SelectWeightedCombo(_currentPhase.AvailableCombos);
            if (selectedCombo == null) return;

            _comboCoroutine = StartCoroutine(ExecuteComboRoutine(selectedCombo));
        }

        private IEnumerator ExecuteComboRoutine(BossCombo combo)
        {
            _isPerformingCombo = true;
            _navAgent.isStopped = true;

            float speedMult = _currentPhase != null ? _currentPhase.AttackSpeedMultiplier : 1f;

            for (int i = 0; i < combo.Moves.Count; i++)
            {
                if (_isDead || _playerTarget == null) break;

                BossAttackMove move = combo.Moves[i];

                // Quay mặt về player trước mỗi đòn
                LookAtPlayer();

                // Play animation
                _vatAnimator.Play(move.AnimClip);

                // Lunge (nếu có) — boss lao về phía player
                if (move.LungeDistance > 0f)
                {
                    StartCoroutine(LungeRoutine(move.LungeDistance, move.WindupTime * (1f / speedMult)));
                }

                // Chờ windup (scale theo attack speed)
                yield return new WaitForSeconds(move.WindupTime * (1f / speedMult));

                // GÂY DAMAGE
                if (!_isDead && _playerTarget != null)
                {
                    PerformHit(move);
                }

                // Chờ recovery trước đòn tiếp (scale theo attack speed)
                yield return new WaitForSeconds(move.RecoveryTime * (1f / speedMult));
            }

            // Combo xong → cooldown
            _isPerformingCombo = false;
            _comboCooldownGate = new TimeGate(combo.CooldownAfterCombo * (1f / speedMult));

            // Quay lại idle
            if (!_isDead)
            {
                _vatAnimator.CrossFade(_idleClip, 0.2f);
            }
        }

        private void PerformHit(BossAttackMove move)
        {
            Vector3 hitCenter = transform.position + transform.forward * move.HitForwardOffset;
            int hits = Physics.OverlapSphereNonAlloc(hitCenter, move.HitRadius, _hitResults, _playerLayerMask);

            for (int i = 0; i < hits; i++)
            {
                if (_hitResults[i].TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(move.Damage * _threatMultiplier);

                    // Spawn hit VFX
                    if (!string.IsNullOrEmpty(move.HitVFXId))
                    {
                        ObjectPoolManager.Instance.Spawn(move.HitVFXId, _hitResults[i].transform.position, Quaternion.identity);
                    }
                }
            }
        }

        private IEnumerator LungeRoutine(float distance, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 lungeDir = transform.forward;
            Vector3 targetPos = startPos + lungeDir * distance;
            float timer = 0f;

            _navAgent.enabled = false;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                // Ease out — nhanh đầu, chậm cuối
                float eased = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            _navAgent.enabled = true;
        }

        private BossCombo SelectWeightedCombo(List<BossCombo> combos)
        {
            int totalWeight = 0;
            foreach (var c in combos) totalWeight += c.SelectionWeight;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var c in combos)
            {
                cumulative += c.SelectionWeight;
                if (roll < cumulative) return c;
            }

            return combos[combos.Count - 1];
        }

        #endregion

        #region Phase System

        private void EvaluatePhase()
        {
            if (_phases.Count == 0) return;

            float healthPercent = _currentHealth / (_maxHealth * _threatMultiplier);

            // Tìm phase phù hợp với HP hiện tại (threshold cao nhất mà HP đã xuống dưới)
            int targetPhase = 0;
            for (int i = _phases.Count - 1; i >= 0; i--)
            {
                if (healthPercent <= _phases[i].HealthThreshold)
                {
                    targetPhase = i;
                    break;
                }
            }

            if (targetPhase != _currentPhaseIndex)
            {
                TransitionToPhase(targetPhase);
            }
        }

        private void TransitionToPhase(int newPhaseIndex)
        {
            int oldPhaseIndex = _currentPhaseIndex;
            _currentPhaseIndex = newPhaseIndex;
            _currentPhase = _phases[_currentPhaseIndex];

            // Áp dụng speed multiplier của phase mới
            _navAgent.speed = _baseMoveSpeed * _currentPhase.MoveSpeedMultiplier;

            // Phase transition animation (skip cho phase đầu tiên)
            if (oldPhaseIndex >= 0 && !string.IsNullOrEmpty(_currentPhase.PhaseTransitionClip))
            {
                StartCoroutine(PhaseTransitionRoutine());
            }

            OnPhaseChanged?.Invoke(_currentPhaseIndex, _currentPhase.PhaseName);
        }

        private IEnumerator PhaseTransitionRoutine()
        {
            _isTransitioning = true;

            // Dừng combo nếu đang thực hiện
            if (_comboCoroutine != null)
            {
                StopCoroutine(_comboCoroutine);
                _isPerformingCombo = false;
            }

            _navAgent.isStopped = true;

            // Spawn VFX chuyển phase
            if (!string.IsNullOrEmpty(_currentPhase.TransitionVFXId))
            {
                ObjectPoolManager.Instance.Spawn(_currentPhase.TransitionVFXId, transform.position, Quaternion.identity);
            }

            // Play transition animation (gầm, biến hình, v.v.)
            _vatAnimator.Play(_currentPhase.PhaseTransitionClip);

            yield return new WaitForSeconds(_currentPhase.TransitionDuration);

            _isTransitioning = false;
            _vatAnimator.CrossFade(_idleClip, 0.2f);
        }

        #endregion

        #region Damage & Death

        public void TakeDamage(float amount)
        {
            if (_isDead || _isTransitioning) return; // Bất khả xâm phạm khi chuyển phase

            _currentHealth -= amount;

            // Play hit animation (không ngắt combo — boss chịu đòn nhưng vẫn đánh tiếp)
            // Chỉ play hit khi KHÔNG đang combo để tránh ngắt animation
            if (!_isPerformingCombo)
            {
                _vatAnimator.Play(_hitClip);
            }

            OnBossHealthChanged?.Invoke(
                Mathf.Max(0, _currentHealth),
                _maxHealth * _threatMultiplier
            );

            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Kiểm tra chuyển phase
                EvaluatePhase();
            }
        }

        private void Die()
        {
            _isDead = true;

            if (_comboCoroutine != null)
            {
                StopCoroutine(_comboCoroutine);
            }

            _navAgent.enabled = false;
            GetComponent<Collider>().enabled = false;
            _vatAnimator.CrossFade(_deathClip, 0.1f);

            ObjectPoolManager.Instance.Spawn(_deathExplosionFXId, transform.position, Quaternion.identity);
            OnBossDeath?.Invoke(this);

            Invoke(nameof(ReturnToPool), 2.5f);
        }

        private void ReturnToPool()
        {
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        #endregion

        #region IPoolableObject

        public void OnObjectSpawn()
        {
            _isDead = false;
            _isPerformingCombo = false;
            _isTransitioning = false;
            _currentPhaseIndex = -1;
            _navAgent.enabled = true;
            _navAgent.isStopped = true;
            GetComponent<Collider>().enabled = true;
            GameManager.Instance.RegisterEnemy(gameObject);
        }

        public void OnObjectReturn()
        {
            if (!_isDead)
            {
                GameManager.Instance.UnregisterEnemy(gameObject);
            }

            if (_comboCoroutine != null)
            {
                StopCoroutine(_comboCoroutine);
            }

            _playerTarget = null;
            _navAgent.enabled = false;
            OnBossDeath = null;
            OnBossHealthChanged = null;
            OnPhaseChanged = null;
        }

        #endregion

        public float HealthPercent => _currentHealth / (_maxHealth * _threatMultiplier);

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            // Chase range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _chaseRange);
        }

        #endregion
    }
}