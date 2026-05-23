using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Не удалось вступить в чат по приглашению
/// </summary>
/// <param name="reason">Причина отказа</param>
public sealed class TelegramJoinChatFailedException(string reason)
    : DomainException($"Не удалось вступить в чат: {reason}");
