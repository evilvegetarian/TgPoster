namespace TgPoster.API.Domain.UseCases.Messages.RetryCrossPost;

public interface IRetryCrossPostStorage
{
	Task<CrossPostRetryInfo?> GetRetryInfoAsync(
		Guid messageId,
		Guid crossPostId,
		Guid userId,
		CancellationToken ct);

	Task RetryAsync(Guid crossPostId, DateTimeOffset now, CancellationToken ct);
}
