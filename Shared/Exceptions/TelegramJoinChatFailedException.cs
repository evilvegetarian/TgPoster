namespace Shared.Exceptions;

/// <summary>
///     Не удалось вступить в чат по приглашению.
/// </summary>
public sealed class TelegramJoinChatFailedException(string reason)
    : SharedException($"Не удалось вступить в чат: {reason}");
