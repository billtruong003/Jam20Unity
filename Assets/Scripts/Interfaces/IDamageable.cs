namespace EchoMage.Interfaces
{
    /// <summary>
    /// Định nghĩa một đối tượng có khả năng nhận sát thương.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Xử lý việc nhận một lượng sát thương.
        /// </summary>
        /// <param name="damageAmount">Lượng sát thương phải nhận.</param>
        void TakeDamage(float damageAmount);
    }
}