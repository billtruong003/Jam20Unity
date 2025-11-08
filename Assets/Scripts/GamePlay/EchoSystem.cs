using EchoMage.Player;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

namespace EchoMage.Echoes
{
    [System.Serializable]
    public class EchoData
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
    }

    [System.Serializable]
    public class EchoSaveData
    {
        public List<EchoData> Echoes = new List<EchoData>();
    }

    public static class EchoSystem
    {
        private static readonly string _savePath = Path.Combine(Application.persistentDataPath, "echo_collection.json");

        public static void AddEcho(PlayerStats stats, Vector3 position)
        {
            EchoSaveData saveData = LoadEchoSaveData();

            var newEcho = new EchoData
            {
                EchoId = System.Guid.NewGuid().ToString(),
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

            saveData.Echoes.Add(newEcho);
            SaveEchoData(saveData);
        }

        public static List<EchoData> LoadAllEchoes()
        {
            return LoadEchoSaveData().Echoes;
        }

        public static void RemoveEcho(string echoId)
        {
            EchoSaveData saveData = LoadEchoSaveData();
            EchoData echoToRemove = saveData.Echoes.FirstOrDefault(e => e.EchoId == echoId);

            if (echoToRemove != null)
            {
                saveData.Echoes.Remove(echoToRemove);
                SaveEchoData(saveData);
            }
        }

        private static EchoSaveData LoadEchoSaveData()
        {
            if (!File.Exists(_savePath))
            {
                return new EchoSaveData();
            }

            string json = File.ReadAllText(_savePath);
            if (string.IsNullOrEmpty(json))
            {
                return new EchoSaveData();
            }

            return JsonUtility.FromJson<EchoSaveData>(json);
        }

        private static void SaveEchoData(EchoSaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(_savePath, json);
        }
    }
}