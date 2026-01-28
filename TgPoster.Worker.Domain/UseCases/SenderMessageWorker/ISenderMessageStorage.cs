namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public interface ISenderMessageStorage
{
	Task<List<MessageDetail>> GetMessagesAsync();
	Task UpdateSendStatusMessageAsync(Guid id);
	Task UpdateStatusInHandleMessageAsync(List<Guid> ids);

	Task UpdateYouTubeTokensAsync(
		Guid youTubeAccountId,
		string accessToken,
		string? refreshToken,
		CancellationToken ct
	);

	Task SaveTelegramMessageIdAsync(Guid messageId, int telegramMessageId, CancellationToken ct);
	Task<List<RepostSettingsDto>> GetRepostSettingsForMessageAsync(Guid messageId, CancellationToken ct);
}

public sealed class RepostSettingsDto
{
	public required Guid Id { get; init; }
	public required Guid ScheduleId { get; init; }
	public required Guid TelegramSessionId { get; init; }
	public List<RepostDestinationDto> Destinations { get; init; } = [];
}

public sealed class RepostDestinationDto
{
	public required Guid Id { get; init; }
	public required long ChatIdentifier { get; init; }
}