using UnityEngine;
using Sirenix.OdinInspector;
using System;
using EchoMage.Loot; // Namespace của StatUpgradeData
using EchoMage.Loot.Effects; // Namespace của StatUpgradeEffect

namespace EchoMage.Player
{
    public enum StatTarget
    {
        MaxHP,
        Damage,
        AttackCooldown,
        ProjectilesPerShot,
        ProjectileSpeed,
        PierceCount,
        ProjectileScale,
        ProjectileLifetime,
        ProjectileSpreadAngle
    }

    public sealed class PlayerStats : SerializedMonoBehaviour
    {
        public event Action OnStatsChanged;

        #region Stats Fields
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
        #endregion

        #region Public API for Upgrades

        /// <summary>
        /// Áp dụng nâng cấp từ hệ thống PickupEffect mới.
        /// </summary>
        public void ApplyUpgrade(StatUpgradeEffect upgradeEffect)
        {
            if (upgradeEffect == null) return;
            ApplyUpgradeInternal(upgradeEffect.StatToUpgrade, upgradeEffect.UpgradeType, upgradeEffect.Value);
        }

        /// <summary>
        /// (Tương thích ngược) Áp dụng nâng cấp từ hệ thống StatUpgradeData cũ.
        /// </summary>
        public void ApplyUpgrade(StatUpgradeData upgradeData)
        {
            if (upgradeData == null) return;
            ApplyUpgradeInternal(upgradeData.StatToUpgrade, upgradeData.UpgradeType, upgradeData.Value);
        }

        /// <summary>
        /// Một phương thức công khai để buộc phát sự kiện OnStatsChanged.
        /// Hữu ích cho các hệ thống thay đổi chỉ số trực tiếp (như EchoGrave).
        /// </summary>
        public void ForceStatsUpdate()
        {
            InvokeStatsChanged();
        }

        #endregion

        #region Internal Logic

        /// <summary>
        /// Phương thức lõi chứa logic nâng cấp, tránh lặp lại code.
        /// </summary>
        private void ApplyUpgradeInternal(StatTarget statToUpgrade, StatUpgradeType upgradeType, float value)
        {
            switch (statToUpgrade)
            {
                case StatTarget.MaxHP:
                    ApplyStatChange(ref MaxHP, value, upgradeType);
                    if (TryGetComponent<PlayerHealth>(out var health)) health.Heal(value);
                    break;
                case StatTarget.Damage:
                    ApplyStatChange(ref Damage, value, upgradeType);
                    break;
                case StatTarget.AttackCooldown:
                    ApplyStatChange(ref AttackCooldown, value, upgradeType);
                    break;
                case StatTarget.ProjectilesPerShot:
                    ProjectilesPerShot += (int)value;
                    break;
                case StatTarget.ProjectileSpeed:
                    ApplyStatChange(ref ProjectileSpeed, value, upgradeType);
                    break;
                case StatTarget.PierceCount:
                    PierceCount += (int)value;
                    break;
                case StatTarget.ProjectileScale:
                    ApplyStatChange(ref ProjectileScale, value, upgradeType);
                    break;
                case StatTarget.ProjectileLifetime:
                    ApplyStatChange(ref ProjectileLifetime, value, upgradeType);
                    break;
                case StatTarget.ProjectileSpreadAngle:
                    ApplyStatChange(ref ProjectileSpreadAngle, value, upgradeType);
                    break;
            }

            InvokeStatsChanged();
        }

        private void ApplyStatChange(ref float stat, float value, StatUpgradeType type)
        {
            if (type == StatUpgradeType.Additive)
            {
                stat += value;
            }
            else if (type == StatUpgradeType.Multiplicative)
            {
                stat *= value;
            }
        }

        private void InvokeStatsChanged()
        {
            OnStatsChanged?.Invoke();
        }
        #endregion
    }
}