namespace TgPoster.Storage.Data.Entities;

public class ChannelParsingParameters : BaseEntity
{
    /// <summary>
    ///     Канал c которого будут парсится сообщения.
    /// </summary>
    public required long ChannelId { get; set; }

    /// <summary>
    /// Нужно ли проверять новые посты.
    /// </summary>
    public bool CheckNewPosts { get; set; }

    /// <summary>
    /// Расписание.
    /// </summary>
    public required Guid ScheduleId { get; set; }

    /// <summary>
    /// Удалять текста у сообщения.
    /// </summary>
    public bool DeleteText { get; set; }

    /// <summary>
    /// Удалять медиа у сообщения.
    /// </summary>
    public bool DeleteMedia { get; set; }

    /// <summary>
    /// Избегать посты с этими словами или предложениями.
    /// </summary>
    public string[] AvoidWords { get; set; } = [];

    /// <summary>
    /// Посты нужно верифицировать дополнительно
    /// </summary>
    public bool NeedVerifiedPosts { get; set; }

    /// <summary>
    /// Дата откуда парсить
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// До какой даты парсить
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Последний id поста, который парсили
    /// </summary>
    public long? LastParseId { get; set; }

    public Schedule Schedule { get; set; } = null!;
}