using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Некорректный формат ссылки на чат
/// </summary>
/// <param name="input">Входная строка с ссылкой</param>
public sealed class TelegramInvalidChatLinkException(string input)
    : DomainException($"Не удалось распознать формат ссылки: {input}");
