using UnityEngine;
using EchoMage.Player;

namespace EchoMage.Loot.Effects
{
    [CreateAssetMenu(fileName = "New Stat Upgrade Effect", menuName = "EchoMage/Pickup Effects/Stat Upgrade")]
    public class StatUpgradeEffect : PickupEffect
    {
        [Header("Stat Upgrade Configuration")]
        public StatTarget StatToUpgrade;
        public StatUpgradeType UpgradeType;
        public float Value;

        public override void Apply(GameObject target)
        {
            if (target.TryGetComponent<PlayerStats>(out var playerStats))
            {
                playerStats.ApplyUpgrade(this);
            }
        }
    }
}