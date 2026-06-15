using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Нет прав на отправку медиа в чат
/// </summary>
/// <param name="chatTitle">Название чата</param>
public sealed class TelegramChatNoMediaPermissionException(string chatTitle)
    : DomainException($"Нет прав на отправку медиа в чат: {chatTitle}");
