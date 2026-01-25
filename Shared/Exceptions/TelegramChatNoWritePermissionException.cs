namespace Shared.Exceptions;

/// <summary>
///     Нет прав на отправку сообщений в чат.
/// </summary>
public sealed class TelegramChatNoWritePermissionException(string chatTitle)
    : SharedException($"Нет прав на отправку сообщений в чат: {chatTitle}");
