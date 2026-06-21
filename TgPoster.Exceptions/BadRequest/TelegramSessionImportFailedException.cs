using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда импорт Telegram сессии из файла не удался
/// </summary>
/// <param name="reason">Причина неудачи импорта</param>
public sealed class TelegramSessionImportFailedException(string reason)
	: DomainException($"Не удалось импортировать Telegram сессию: {reason}");