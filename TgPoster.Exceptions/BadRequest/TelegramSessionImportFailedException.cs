namespace TgPoster.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда импорт Telegram сессии из файла не удался
/// </summary>
/// <param name="reason">Причина неудачи импорта</param>
public sealed class TelegramSessionImportFailedException(string reason)
	: DomainException($"Не удалось импортировать Telegram сессию: {reason}");
