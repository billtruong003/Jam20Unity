using System;
using UnityEngine;

namespace EchoMage.Core
{
    /// <summary>
    /// Hệ thống Difficulty kiểu Sifu — không chỉ scale tuyến tính mà TĂNG TỐC theo thời gian.
    /// 
    /// Triết lý Sifu:
    /// - 0-3 phút: Dễ thở, để player làm quen → multiplier ~1.0x
    /// - 3-6 phút: Bắt đầu nóng, quái nhanh hơn, nhiều hơn → ~1.3x
    /// - 6-9 phút: Áp lực rõ ràng, phải chơi tốt → ~1.8x
    /// - 9-12 phút: Gần boss, cực kỳ nguy hiểm → ~2.5x
    /// - 12+ phút: Endless brutality, scale mạnh → 3.0x+
    /// 
    /// Curve này áp dụng cho: HP quái, damage quái, tốc độ spawn, số lượng quái.
    /// Mỗi stat có curve RIÊNG để balance độc lập.
    /// 
    /// Setup trong Inspector:
    /// - Kéo AnimationCurve → chỉnh đồ thị trực quan
    /// - Trục X = thời gian đã qua (normalized 0-1 trong gameDuration)
    /// - Trục Y = hệ số nhân (1.0 = bình thường, 2.0 = gấp đôi)
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        public event Action<float> OnDifficultyChanged;

        [Header("Timing")]
        [Tooltip("Tổng thời gian (giây) để curve đi từ 0% đến 100%. Mặc định 900 = 15 phút.")]
        [SerializeField] private float gameDuration = 900f;

        [Header("Difficulty Curves")]
        [Tooltip("Curve cho HP quái. X = thời gian (0-1), Y = hệ số nhân HP.")]
        [SerializeField]
        private AnimationCurve enemyHealthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),       // 0 phút: 1.0x
            new Keyframe(0.2f, 1.1f),   // 3 phút: 1.1x — hầu như không khác
            new Keyframe(0.4f, 1.4f),   // 6 phút: 1.4x — bắt đầu cảm nhận
            new Keyframe(0.6f, 1.9f),   // 9 phút: 1.9x — rõ ràng khó hơn
            new Keyframe(0.8f, 2.6f),   // 12 phút: 2.6x — rất trâu
            new Keyframe(1f, 3.5f)      // 15 phút: 3.5x — brutal
        );

        [Tooltip("Curve cho damage quái. Leo nhanh hơn HP để tạo áp lực.")]
        [SerializeField]
        private AnimationCurve enemyDamageCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.2f, 1.15f),
            new Keyframe(0.4f, 1.5f),
            new Keyframe(0.6f, 2.2f),   // Damage scale nhanh hơn HP
            new Keyframe(0.8f, 3.0f),
            new Keyframe(1f, 4.0f)
        );

        [Tooltip("Curve cho tốc độ di chuyển quái. Nhẹ nhàng hơn để không quá bất công.")]
        [SerializeField]
        private AnimationCurve enemySpeedCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 1.05f),
            new Keyframe(0.6f, 1.15f),
            new Keyframe(0.8f, 1.25f),
            new Keyframe(1f, 1.35f)     // Tối đa +35% speed — nhiều hơn sẽ bất công
        );

        [Tooltip("Curve cho tốc độ spawn (nhân với spawn interval — giá trị THẤP = spawn nhanh hơn).")]
        [SerializeField]
        private AnimationCurve spawnRateCurve = new AnimationCurve(
            new Keyframe(0f, 1f),       // Bình thường
            new Keyframe(0.2f, 0.9f),   // 10% nhanh hơn
            new Keyframe(0.4f, 0.75f),  // 25% nhanh hơn
            new Keyframe(0.6f, 0.6f),   // 40% nhanh hơn
            new Keyframe(0.8f, 0.45f),  // Gần gấp đôi tốc độ spawn
            new Keyframe(1f, 0.35f)     // Spawn dồn dập
        );

        [Tooltip("Curve cho số lượng quái mỗi wave (nhân với count gốc).")]
        [SerializeField]
        private AnimationCurve spawnCountCurve = new AnimationCurve(
            new Keyframe(0f, 0.6f),     // Ban đầu giảm 40% (giải quyết vấn đề quá nhiều quái)
            new Keyframe(0.2f, 0.7f),
            new Keyframe(0.4f, 0.85f),
            new Keyframe(0.6f, 1.0f),   // Trở về số lượng gốc
            new Keyframe(0.8f, 1.2f),
            new Keyframe(1f, 1.5f)      // 50% nhiều hơn gốc
        );

        [Header("Death Penalty — Kiểu Sifu")]
        [Tooltip("Mỗi lần chết, difficulty sẽ nhảy thêm bao nhiêu % (0.05 = 5%).")]
        [SerializeField] private float deathPenaltyPercent = 0.05f;

        [Tooltip("Số lần chết tối đa được tính penalty (tránh snowball quá mạnh).")]
        [SerializeField] private int maxDeathPenaltyCount = 5;

        // Runtime state
        private float _gameTimer = 0f;
        private int _deathCount = 0;
        private float _currentDifficultyPercent = 0f;
        private bool _isRunning = false;

        // Cached values — các system khác đọc trực tiếp
        public float EnemyHealthMultiplier { get; private set; } = 1f;
        public float EnemyDamageMultiplier { get; private set; } = 1f;
        public float EnemySpeedMultiplier { get; private set; } = 1f;
        public float SpawnRateMultiplier { get; private set; } = 1f;
        public float SpawnCountMultiplier { get; private set; } = 0.6f;

        /// <summary>
        /// % difficulty hiện tại (0-1+). Có thể > 1 do death penalty.
        /// </summary>
        public float DifficultyPercent => _currentDifficultyPercent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!_isRunning) return;

            _gameTimer += Time.deltaTime;
            UpdateDifficulty();
        }

        /// <summary>
        /// Bắt đầu/reset timer difficulty. Gọi khi bắt đầu game hoặc continue.
        /// </summary>
        public void StartDifficulty()
        {
            _gameTimer = 0f;
            _deathCount = 0;
            _isRunning = true;
            UpdateDifficulty();
        }

        /// <summary>
        /// Gọi khi player chết — tăng difficulty penalty kiểu Sifu.
        /// Càng chết nhiều → game càng khó → buộc player phải chơi tốt hơn.
        /// </summary>
        public void NotifyPlayerDeath()
        {
            _deathCount = Mathf.Min(_deathCount + 1, maxDeathPenaltyCount);
            UpdateDifficulty();
        }

        /// <summary>
        /// Tạm dừng timer (khi pause).
        /// </summary>
        public void Pause() => _isRunning = false;

        /// <summary>
        /// Tiếp tục timer.
        /// </summary>
        public void Resume() => _isRunning = true;

        /// <summary>
        /// Reset hoàn toàn (new game).
        /// </summary>
        public void ResetDifficulty()
        {
            _gameTimer = 0f;
            _deathCount = 0;
            _isRunning = false;
            UpdateDifficulty();
        }

        private void UpdateDifficulty()
        {
            // Time-based progress (0 → 1 trong gameDuration, tiếp tục > 1 sau đó)
            float timePercent = gameDuration > 0 ? _gameTimer / gameDuration : 0f;

            // Death penalty: mỗi lần chết đẩy difficulty lên thêm
            float deathBonus = _deathCount * deathPenaltyPercent;

            // Tổng difficulty percent — có thể > 1.0 (curve sẽ extrapolate hoặc clamp)
            _currentDifficultyPercent = timePercent + deathBonus;

            // Evaluate mỗi curve
            // Clamp01 cho các curve không muốn extrapolate quá mạnh
            float t = _currentDifficultyPercent;

            EnemyHealthMultiplier = enemyHealthCurve.Evaluate(Mathf.Min(t, 1.5f));
            EnemyDamageMultiplier = enemyDamageCurve.Evaluate(Mathf.Min(t, 1.5f));
            EnemySpeedMultiplier = enemySpeedCurve.Evaluate(Mathf.Min(t, 1.5f));
            SpawnRateMultiplier = spawnRateCurve.Evaluate(Mathf.Min(t, 1.5f));
            SpawnCountMultiplier = spawnCountCurve.Evaluate(Mathf.Min(t, 1.5f));

            // Đảm bảo không âm
            EnemyHealthMultiplier = Mathf.Max(0.1f, EnemyHealthMultiplier);
            EnemyDamageMultiplier = Mathf.Max(0.1f, EnemyDamageMultiplier);
            EnemySpeedMultiplier = Mathf.Max(0.5f, EnemySpeedMultiplier);
            SpawnRateMultiplier = Mathf.Max(0.1f, SpawnRateMultiplier);
            SpawnCountMultiplier = Mathf.Max(0.3f, SpawnCountMultiplier);

            OnDifficultyChanged?.Invoke(_currentDifficultyPercent);
        }
    }
}