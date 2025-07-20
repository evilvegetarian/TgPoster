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
    ///     Канал на который будет отправляться сообщения
    /// </summary>
    public required long ChannelId { get; set; }

    /// <summary>
    ///     Название  канала на который будет отправляться сообщения
    /// </summary>
    public required string ChannelName { get; set; }

    /// <summary>
    ///     Обозначает активность канала
    /// </summary>
    public bool IsActive { get; set; }

    #region Навигация

    /// <summary>
    ///     Телеграм бот.
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

    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<ChannelParsingParameters> Parameters { get; set; } = [];

    #endregion
}