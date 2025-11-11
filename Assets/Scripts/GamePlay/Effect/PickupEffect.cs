using UnityEngine;

namespace EchoMage.Loot.Effects
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho tất cả các hiệu ứng có thể được áp dụng
    /// khi nhặt một vật phẩm. Mỗi hiệu ứng cụ thể (hồi máu, nâng cấp chỉ số)
    /// sẽ kế thừa từ lớp này.
    /// </summary>
    public abstract class PickupEffect : ScriptableObject
    {
        /// <summary>
        /// Áp dụng hiệu ứng này lên một đối tượng mục tiêu.
        /// </summary>
        /// <param name="target">Đối tượng sẽ nhận hiệu ứng (thường là người chơi).</param>
        public abstract void Apply(GameObject target);
    }
}