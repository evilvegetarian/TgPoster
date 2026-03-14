namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(CancellationToken ct);

	Task<bool> ExistsAsync(string username, CancellationToken ct);

	Task UpsertAsync(
		string username,
		string? sourceUrl,
		int? lastParsedId,
		long? telegramId,
		CancellationToken ct);

	Task UpdateLastParsedIdAsync(
		string username,
		int lastParsedId,
		long telegramId,
		CancellationToken ct);
}
