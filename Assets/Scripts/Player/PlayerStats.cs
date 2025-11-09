using UnityEngine;
using Sirenix.OdinInspector;
using System;
using EchoMage.Loot; // Thêm namespace

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

        public void ApplyUpgrade(StatUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            float value = upgradeData.Value;
            switch (upgradeData.StatToUpgrade)
            {
                case StatTarget.MaxHP:
                    ApplyStatChange(ref MaxHP, value, upgradeData.UpgradeType);
                    break;
                case StatTarget.Damage:
                    ApplyStatChange(ref Damage, value, upgradeData.UpgradeType);
                    break;
                case StatTarget.AttackCooldown:
                    // Attack cooldown is inverted, lower is better. Multiplicative < 1 means faster.
                    ApplyStatChange(ref AttackCooldown, value, upgradeData.UpgradeType);
                    break;
                case StatTarget.ProjectilesPerShot:
                    ProjectilesPerShot += (int)value;
                    break;
                case StatTarget.ProjectileSpeed:
                    ApplyStatChange(ref ProjectileSpeed, value, upgradeData.UpgradeType);
                    break;
                case StatTarget.PierceCount:
                    PierceCount += (int)value;
                    break;
                case StatTarget.ProjectileScale:
                    ApplyStatChange(ref ProjectileScale, value, upgradeData.UpgradeType);
                    break;
                case StatTarget.ProjectileLifetime:
                    ApplyStatChange(ref ProjectileLifetime, value, upgradeData.UpgradeType);
                    break;
                case StatTarget.ProjectileSpreadAngle:
                    ApplyStatChange(ref ProjectileSpreadAngle, value, upgradeData.UpgradeType);
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
    }
}