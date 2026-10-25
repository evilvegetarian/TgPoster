namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverSettings;

/// <summary>
/// Сохранённые настройки поиска каналов, как они лежат в БД
/// </summary>
public sealed record SavedDiscoverSettingsDto
{
	public required bool IsEnabled { get; init; }
	public required int IntervalMinutes { get; init; }
	public required int ChannelsPerSession { get; init; }
	public DateTimeOffset? UpdatedAt { get; init; }
}

public interface IGetDiscoverSettingsStorage
{
	/// <summary>
	/// Прочитать сохранённые настройки поиска каналов; null, если их ещё нет
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<SavedDiscoverSettingsDto?> GetDiscoverSettingsAsync(CancellationToken ct);

	/// <summary>
	/// Сессии для выбора: все сессии пользователя и назначенные поиску каналов сессии других пользователей
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverSessionOption>> GetDiscoverSessionsAsync(Guid userId, CancellationToken ct);
}
