namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	Task<bool> ExistsAsync(string username, CancellationToken ct);

	Task UpsertAsync(
		string username,
		string? sourceUrl,
		int? lastParsedId,
		CancellationToken ct);
}
