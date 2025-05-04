using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

public class Message : BaseEntity
{
    /// <summary>
    ///     Id расписания
    /// </summary>
    public required Guid ScheduleId { get; init; }

    /// <summary>
    ///     Время поста
    /// </summary>
    public required DateTimeOffset TimePosting { get; init; }

    /// <summary>
    ///     Текстовое сообщение
    /// </summary>
    public string? TextMessage { get; init; }

    /// <summary>
    ///     Если в TextMessage больше 1024 символов, в нем не могут быть файлы,
    ///     Если меньше или равно 1024 то, текст становится caption к первому файлу.
    /// </summary>
    public required bool IsTextMessage { get; init; }

    /// <summary>
    ///     Статус сообщения
    /// </summary>
    public MessageStatus Status { get; init; }

    /// <summary>
    ///     Сообщение верифицировано
    ///     Дефолтно ставится true
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    ///     Расписание
    /// </summary>
    public Schedule Schedule { get; init; } = null!;

    /// <summary>
    ///     Файлы сообщения
    /// </summary>
    public ICollection<MessageFile> MessageFiles { get; init; } = [];
}

public static class MessageExtenstion
{
    public static bool IsTextMessage(this string? text)
    {
        return text?.Length >= 1024;
    }
}