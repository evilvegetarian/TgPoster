namespace TgPoster.Exceptions;

/// <summary>
///     Чат не найден по указанной ссылке или ID
/// </summary>
/// <param name="input">Входная строка с ссылкой или ID</param>
public sealed class TelegramChatNotFoundException(string input)
    : DomainException($"Чат не найден: {input}. Если чат приватный. Введите пригласительную ссылку или сами вступите в данный чат.");

/// <summary>
///     Чат заблокирован для этого пользователя
/// </summary>
public sealed class TelegramChatForbidden()
	: DomainException($"Чат заблокирован для этого пользователя");
