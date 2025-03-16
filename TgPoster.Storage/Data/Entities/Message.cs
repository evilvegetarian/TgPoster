using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

public class Message : BaseEntity
{
    /// <summary>
    ///     Id расписания
    /// </summary>
    public required Guid ScheduleId { get; set; }

    /// <summary>
    ///     Время поста
    /// </summary>
    public required DateTimeOffset TimePosting { get; set; }

    /// <summary>
    ///     Текстовое сообщение
    /// </summary>
    public string? TextMessage { get; set; }

    /// <summary>
    ///     Если в TextMessage больше 1024 символов, в нем не могут быть файлы,
    ///     Если меньше или равно 1024 то, текст становится caption к первому файлу.
    /// </summary>
    public required bool IsTextMessage { get; set; }

    /// <summary>
    ///     Статус сообщения
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    ///     Расписание
    /// </summary>
    public Schedule Schedule { get; set; } = null!;

    /// <summary>
    ///     Файлы сообщения
    /// </summary>
    public ICollection<MessageFile> MessageFiles { get; set; } = [];
}

public static class MessageExtenstion
{
    public static bool IsTextMessage(this string? text) => text?.Length >= 1024;
}