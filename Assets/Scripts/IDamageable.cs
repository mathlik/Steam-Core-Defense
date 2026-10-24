/// <summary>
/// Общий интерфейс для объектов, которые могут получать урон.
/// Например, механический паук сможет реализовать этот интерфейс.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Наносит объекту указанное количество урона.
    /// </summary>
    /// <param name="amount">Количество урона.</param>
    void TakeDamage(float amount);
}
