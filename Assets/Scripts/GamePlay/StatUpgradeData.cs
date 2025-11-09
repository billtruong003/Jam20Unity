using UnityEngine;
using EchoMage.Player;

namespace EchoMage.Loot
{
    public enum StatUpgradeType
    {
        Additive,
        Multiplicative
    }

    [CreateAssetMenu(fileName = "NewStatUpgrade", menuName = "EchoMage/Stat Upgrade Data")]
    public class StatUpgradeData : ScriptableObject
    {
        public StatTarget StatToUpgrade;
        public StatUpgradeType UpgradeType;
        public float Value;
    }
}