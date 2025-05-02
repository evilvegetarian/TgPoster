using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

public class ChannelParsingParameters : BaseEntity
{
    /// <summary>
    ///     Канал c которого будут парсится сообщения.
    /// </summary>
    public string Channel { get; set; }

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
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// До какой даты парсить
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Самый последний пост который парсили
    /// </summary>
    public int? LastParseId { get; set; }

    /// <summary>
    /// Статус парсинга
    /// </summary>
    public ParsingStatus Status { get; set; }

    public Schedule Schedule { get; set; } = null!;
}