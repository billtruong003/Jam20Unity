using System;
using UnityEngine;

namespace EchoMage.Echoes
{
    /// <summary>
    /// Một cấu trúc dữ liệu đơn giản để lưu trữ trạng thái của người chơi tại thời điểm chết.
    /// Chỉ tồn tại trong bộ nhớ, không lưu vào file.
    /// </summary>
    [Serializable]
    public sealed class PlayerEchoData
    {
        public string EchoId;
        public Vector3 DeathPosition;
        public float Damage;
        public float AttackCooldown;
        public int ProjectilesPerShot;
        public float ProjectileSpeed;
        public int PierceCount;
        public float ProjectileScale;
        public float ProjectileLifetime;
        public float ProjectileSpreadAngle;

        public PlayerEchoData(Player.PlayerStats stats, Vector3 position)
        {
            EchoId = System.Guid.NewGuid().ToString();
            DeathPosition = position;
            Damage = stats.Damage;
            AttackCooldown = stats.AttackCooldown;
            ProjectilesPerShot = stats.ProjectilesPerShot;
            ProjectileSpeed = stats.ProjectileSpeed;
            PierceCount = stats.PierceCount;
            ProjectileScale = stats.ProjectileScale;
            ProjectileLifetime = stats.ProjectileLifetime;
            ProjectileSpreadAngle = stats.ProjectileSpreadAngle;
        }
    }
}