namespace TgPoster.Storage.Data.Entities;

public sealed class Schedule : BaseEntity
{
    /// <summary>
    ///     Наименование расписания.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Id пользователя
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    ///     Id телеграм бота.
    /// </summary>
    public required Guid TelegramBotId { get; set; }

    /// <summary>
    ///     телеграм бот.
    /// </summary>
    public TelegramBot TelegramBot { get; set; } = null!;

    /// <summary>
    ///     Пользователь.
    /// </summary>
    //TODO: Вынести отдельно в Many-to-Many в будущем 
    public User User { get; set; } = null!;

    /// <summary>
    ///     Дни постинга.
    /// </summary>
    public ICollection<Day> Days { get; set; } = [];
}