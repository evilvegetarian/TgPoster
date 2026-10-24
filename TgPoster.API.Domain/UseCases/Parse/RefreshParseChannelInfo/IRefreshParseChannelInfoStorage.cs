namespace TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;

public interface IRefreshParseChannelInfoStorage
{
	/// <summary>
	///     Получает TelegramSessionId и имя канала по Id настройки парсинга пользователя.
	/// </summary>
	Task<(Guid TelegramSessionId, string Channel)?> GetParseChannelInfoAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Обновляет общее количество сообщений в канале.
	/// </summary>
	Task UpdateTotalMessagesCountAsync(Guid id, int? totalMessagesCount, CancellationToken ct);
}