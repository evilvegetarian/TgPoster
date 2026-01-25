namespace TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

public interface IRepostMessageConsumerStorage
{
	Task<RepostDataDto?> GetRepostDataAsync(Guid messageId, CancellationToken ct);

	Task CreateRepostLogAsync(
		Guid messageId,
		Guid repostDestinationId,
		int? telegramMessageId,
		string? error,
		CancellationToken ct);
}

public sealed class RepostDataDto
{
	public required int? TelegramMessageId { get; init; }
	public required Guid TelegramSessionId { get; init; }
	public required string SourceChannelIdentifier { get; init; }
	public List<RepostDestinationDataDto> Destinations { get; init; } = [];
}

public sealed class RepostDestinationDataDto
{
	public required Guid Id { get; init; }
	public required long ChatIdentifier { get; init; }
}
