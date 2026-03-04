namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда импорт Telegram сессии из файла не удался.
/// </summary>
public sealed class TelegramSessionImportFailedException(string reason)
	: SharedException($"Не удалось импортировать Telegram сессию: {reason}");
