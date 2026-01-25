namespace Shared.Exceptions;

/// <summary>
///     Некорректный формат ссылки на чат.
/// </summary>
public sealed class TelegramInvalidChatLinkException(string input)
    : SharedException($"Не удалось распознать формат ссылки: {input}");
