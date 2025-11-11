using UnityEngine;
using EchoMage.Loot; // Thêm namespace

namespace EchoMage.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemyStats", menuName = "EchoMage/Enemy Stats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Core Stats")]
        public float MaxHealth = 100f;
        public float Damage = 10f;
        public float MoveSpeed = 3.5f;

        [Header("Attack Behavior")]
        public float AttackRange = 1.5f;
        public float AttackCooldown = 2f;

        [Header("Loot")] // Thêm mục mới
        public int ScoreValue = 10;
        public LootTableData LootTable;

        [Header("VAT Animation Clips")]
        [Tooltip("Tên của animation clip di chuyển trong VAT_AnimationData")]
        public string MoveClipName = "Move";

        [Tooltip("Tên của animation clip tấn công trong VAT_AnimationData")]
        public string AttackClipName = "Attack";

        [Tooltip("Tên của animation clip chết trong VAT_AnimationData")]
        public string DeathClipName = "Die";

        [Tooltip("Tên của animation clip đứng yên trong VAT_AnimationData")]
        public string IdleClipName = "Idle";

        [Tooltip("Tên của animation clip bị đánh trong VAT_AnimationData")]
        public string HitClipName = "Hit";
    }
}