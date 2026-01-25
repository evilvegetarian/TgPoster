namespace Shared.Exceptions;

/// <summary>
///     Чат не найден по указанной ссылке или ID.
/// </summary>
public sealed class TelegramChatNotFoundException(string input)
    : SharedException($"Чат не найден: {input}. Если чат приватный. Введите пригласительную ссылку или сами вступите в данный чат.");

public sealed class TelegramChatForbidden()
	: SharedException($"Чат заблокирован для этого пользователя");
