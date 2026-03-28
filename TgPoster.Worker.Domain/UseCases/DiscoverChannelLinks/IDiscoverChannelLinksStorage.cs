namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(CancellationToken ct);

	Task<bool> ExistsAsync(string username, CancellationToken ct);

	Task UpsertAsync(
		string username,
		string? tgUrl,
		int? lastParsedId,
		long? telegramId,
		string? peerType,
		string? title,
		int? participantsCount,
		bool markAsCompleted = false,
		CancellationToken ct = default);
}
