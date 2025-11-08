// Assets/Scripts/GamePlay/EchoSystem.cs

using EchoMage.Player;
using System.IO;
using UnityEngine;

namespace EchoMage.Echoes
{
    [System.Serializable]
    public class EchoData
    {
        public Vector3 DeathPosition;
        // Các chỉ số được lưu lại
        public float Damage;
        public float AttackCooldown;
        public int ProjectilesPerShot;
        public float ProjectileSpeed;
        public int PierceCount;
        public float ProjectileScale;
        public float ProjectileLifetime;
        public float ProjectileSpreadAngle;
    }

    public static class EchoSystem
    {
        private static readonly string _savePath = Path.Combine(Application.persistentDataPath, "echo.json");

        public static void SaveEcho(PlayerStats stats, Vector3 position)
        {
            var data = new EchoData
            {
                DeathPosition = position,
                Damage = stats.Damage,
                AttackCooldown = stats.AttackCooldown,
                ProjectilesPerShot = stats.ProjectilesPerShot,
                ProjectileSpeed = stats.ProjectileSpeed,
                PierceCount = stats.PierceCount,
                ProjectileScale = stats.ProjectileScale,
                ProjectileLifetime = stats.ProjectileLifetime,
                ProjectileSpreadAngle = stats.ProjectileSpreadAngle
            };

            string json = JsonUtility.ToJson(data);
            File.WriteAllText(_savePath, json);
            Debug.Log($"Echo saved at: {_savePath}");
        }

        public static EchoData LoadEcho()
        {
            if (!File.Exists(_savePath)) return null;

            string json = File.ReadAllText(_savePath);
            return JsonUtility.FromJson<EchoData>(json);
        }

        public static void ClearEcho()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
        }
    }
}