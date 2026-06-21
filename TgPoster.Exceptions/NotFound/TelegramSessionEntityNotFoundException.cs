using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

/// <summary>
///     Исключение, выбрасываемое когда запись о Telegram-сессии не найдена в базе данных
/// </summary>
/// <param name="id">Идентификатор записи Telegram-сессии</param>
public sealed class TelegramSessionEntityNotFoundException(Guid? id = null)
	: NotFoundException($"Telegram сессия с ID {id} не найдена.");