namespace TgPoster.Storage.Data.Entities;

public sealed class Schedule : BaseEntity
{
    /// <summary>
    /// Id пользователя 
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Наименование расписания
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Пользователь
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Дни постинга
    /// </summary>
    public IEnumerable<Day> Days { get; set; } = [];
}