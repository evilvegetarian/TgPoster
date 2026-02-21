using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

public interface IRepostMessageConsumerStorage
{
	Task<RepostDataDto?> GetRepostDataAsync(Guid messageId, Guid repostSettingsId, CancellationToken ct);

	Task CreateRepostLogAsync(
		Guid messageId,
		Guid repostDestinationId,
		int? telegramMessageId,
		string? error,
		CancellationToken ct);

	/// <summary>
	///     Обновляет статус доступа к целевому каналу.
	/// </summary>
	Task UpdateDestinationStatusAsync(Guid destinationId, ChatStatus chatStatus, CancellationToken ct);

	/// <summary>
	///     Инкрементирует счётчик репостов для destination и возвращает новое значение.
	/// </summary>
	Task<int> IncrementRepostCounterAsync(Guid destinationId, CancellationToken ct);

	/// <summary>
	///     Возвращает количество успешных репостов за сегодня для destination.
	/// </summary>
	Task<int> GetTodayRepostCountAsync(Guid destinationId, CancellationToken ct);
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
	public int DelayMinSeconds { get; init; }
	public int DelayMaxSeconds { get; init; }
	public int RepostEveryNth { get; init; } = 1;
	public int SkipProbability { get; init; }
	public int? MaxRepostsPerDay { get; init; }
}
