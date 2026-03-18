namespace TgPoster.Worker.Domain.UseCases.ScrapeChannel;

public interface IScrapeChannelStorage
{
	Task<Guid> UpsertChannelAsync(
		string username,
		string? title,
		string? description,
		string? avatarUrl,
		int? participantsCount,
		string? peerType,
		string? tgUrl,
		CancellationToken ct);
}
