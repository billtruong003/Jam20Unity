using UnityEngine;
using Sirenix.OdinInspector;
using System; // Cần thêm để sử dụng Action

namespace EchoMage.Player
{
    public sealed class PlayerStats : SerializedMonoBehaviour
    {
        public event Action OnStatsChanged;

        [Title("Health Stats")]
        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(1)]
        public float MaxHP = 100f;

        [Title("Primary Attack Stats")]
        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(0)]
        public float Damage = 10f;

        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(0.01)]
        public float AttackCooldown = 0.5f;

        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(1)]
        public int ProjectilesPerShot = 1;

        [Title("Projectile Behavior")]
        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(0.1)]
        public float ProjectileSpeed = 20f;

        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(1)]
        public int PierceCount = 1;

        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(0.1)]
        public float ProjectileScale = 1f;

        [OnValueChanged("InvokeStatsChanged")]
        [MinValue(0.1)]
        public float ProjectileLifetime = 5f;

        [OnValueChanged("InvokeStatsChanged")]
        [Tooltip("Tổng góc bắn (độ) khi có nhiều hơn 1 viên đạn. 0 = bắn song song.")]
        [Range(0, 360)]
        public float ProjectileSpreadAngle = 15f;

        // Hàm này sẽ được gọi mỗi khi có nâng cấp
        public void ApplyUpgrade()
        {
            // ... logic thay đổi chỉ số ở đây
            // ví dụ: Damage += 5;

            // Sau khi thay đổi, thông báo cho các hệ thống khác
            InvokeStatsChanged();
        }

        private void InvokeStatsChanged()
        {
            OnStatsChanged?.Invoke();
        }
    }
}