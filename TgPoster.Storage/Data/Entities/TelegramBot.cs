namespace TgPoster.Storage.Data.Entities;

public sealed class TelegramBot : BaseEntity
{
    /// <summary>
    ///     Api телеграм бота в зашифрованном виде.
    /// </summary>
    public required string ApiTelegram { get; set; }

    /// <summary>
    ///     Название телеграм бота.
    /// </summary>
    public required string Name { get; set; } 

    /// <summary>
    ///     Чат с самим собой, нужен для сохранения файлов.
    /// </summary>
    public required long ChatId { get; set; }

    /// <summary>
    ///     Id владельца телеграм бота.
    /// </summary>
    public required Guid OwnerId { get; set; }

    /// <summary>
    ///     Владелец телеграм бота.
    /// </summary>
    public User Owner { get; set; } = null!;

    /// <summary>
    ///     Все расписания телеграм бота.
    /// </summary>
    public ICollection<Schedule> Schedules { get; set; } = [];
}