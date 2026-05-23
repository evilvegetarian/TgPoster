using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Нет прав на отправку сообщений в чат
/// </summary>
/// <param name="chatTitle">Название чата</param>
public sealed class TelegramChatNoWritePermissionException(string chatTitle)
    : DomainException($"Нет прав на отправку сообщений в чат: {chatTitle}");
