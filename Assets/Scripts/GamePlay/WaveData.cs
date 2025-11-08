using UnityEngine;

namespace EchoMage.Spawning
{
    [System.Serializable]
    public struct WaveEntry
    {
        public GameObject EnemyPrefab;
        [Min(1)] public int Count;
        [Min(0f)] public float SpawnInterval; // Thời gian chờ giữa mỗi con trong entry này
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "EchoMage/Wave Data")]
    public class WaveData : ScriptableObject
    {
        public WaveEntry[] WaveEntries;
        public float TimeToNextWave = 5f;
    }
}