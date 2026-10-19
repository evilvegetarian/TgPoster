namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	/// <summary>
	/// Каналы на парсинг вместе с сессиями, которые в канале забанены. Каналы, где забанены все переданные
	/// сессии, не возвращаются: их некому парсить, а в очереди они занимали бы место каждый запуск.
	/// Сначала все чаты, потом каналы; внутри каждой группы — ни разу не парсенные, затем самые давние
	/// </summary>
	/// <param name="channelBatchSize"></param>
	/// <param name="sessionIds"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(
		int channelBatchSize,
		IReadOnlyCollection<Guid> sessionIds,
		CancellationToken ct
	);

	Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct);
	Task BulkUpsertAsync(IReadOnlyCollection<DiscoveredPeerUpsert> upserts, CancellationToken ct);
	Task MarkAsSkippedAsync(Guid id, CancellationToken ct);

	/// <summary>
	///     Пометить канал ошибкой: он выпадет из очереди парсинга, дата последнего парсинга не меняется
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	Task MarkAsErrorAsync(Guid id, CancellationToken ct);

	/// <summary>
	/// Канала нет для всех (username не занят): скрыть его из Discover
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task ChannelBanned(Guid id, CancellationToken ct = default);

	/// <summary>
	/// Канал закрыт только этому аккаунту: записать бан за сессией, а канал оставить в Discover для остальных.
	/// Telegram-id канала сохраняется, если его ещё не было, иначе бан не найдётся при следующем отборе
	/// </summary>
	/// <param name="channelId"></param>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkAccountBannedAsync(
		Guid channelId,
		Guid telegramSessionId,
		long chatId,
		string? error,
		CancellationToken ct
	);
}
