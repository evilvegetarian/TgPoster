namespace TgPoster.API.Domain.UseCases.Repost.RestoreRepostDestination;

/// <summary>
/// Удалённый канал, который можно вернуть
/// </summary>
/// <param name="ChatId">ID чата в Telegram</param>
/// <param name="Title">Название чата</param>
public sealed record RemovedRepostDestinationDto(long ChatId, string? Title);

public interface IRestoreRepostDestinationStorage
{
	/// <summary>
	/// Проверяет, что настройки репоста существуют и принадлежат пользователю
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> SettingsExistsAsync(Guid settingsId, Guid userId, CancellationToken ct);

	/// <summary>
	/// Удалённый из списка, но не стёртый насовсем канал настроек; null, если такого нет
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="destinationId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<RemovedRepostDestinationDto?> GetRemovedAsync(Guid settingsId, Guid destinationId, CancellationToken ct);

	/// <summary>
	/// Есть ли этот чат в общем списке настроек
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="chatId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> ChatInListAsync(Guid settingsId, long chatId, CancellationToken ct);

	/// <summary>
	/// Возвращает канал в общий список выключенным
	/// </summary>
	/// <param name="destinationId"></param>
	/// <param name="ct"></param>
	Task RestoreAsync(Guid destinationId, CancellationToken ct);
}
