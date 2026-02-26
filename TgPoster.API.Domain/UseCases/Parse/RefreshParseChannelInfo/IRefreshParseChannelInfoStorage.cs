namespace TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;

public interface IRefreshParseChannelInfoStorage
{
	/// <summary>
	///     Получает TelegramSessionId и имя канала по Id настройки парсинга.
	/// </summary>
	Task<(Guid TelegramSessionId, string Channel)?> GetParseChannelInfoAsync(Guid id, CancellationToken ct);

	/// <summary>
	///     Обновляет общее количество сообщений в канале.
	/// </summary>
	Task UpdateTotalMessagesCountAsync(Guid id, int? totalMessagesCount, CancellationToken ct);
}
