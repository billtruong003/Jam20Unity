using UnityEngine;
using EchoMage.Player;

namespace EchoMage.Loot.Effects
{
    [CreateAssetMenu(fileName = "New Heal Effect", menuName = "EchoMage/Pickup Effects/Heal")]
    public class HealEffect : PickupEffect
    {
        [Header("Heal Configuration")]
        [Min(0)]
        public float HealAmount;

        public override void Apply(GameObject target)
        {
            if (target.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                playerHealth.Heal(HealAmount);
            }
        }
    }
}