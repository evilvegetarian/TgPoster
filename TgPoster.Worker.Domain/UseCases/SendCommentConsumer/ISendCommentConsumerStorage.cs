namespace TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

public interface ISendCommentConsumerStorage
{
	/// <summary>
	///     Создаёт запись в журнале комментирующего репоста.
	///     Если error != null, статус устанавливается как Failed, иначе Success.
	/// </summary>
	Task CreateLogAsync(
		Guid settingsId,
		int originalPostId,
		int? forwardedMessageId,
		int? commentMessageId,
		string? error,
		CancellationToken ct);
}
