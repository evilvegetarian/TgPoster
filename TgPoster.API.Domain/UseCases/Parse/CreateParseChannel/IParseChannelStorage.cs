namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

public interface IParseChannelStorage
{
	Task<Guid> AddParseChannelParametersAsync(
		string channel,
		bool alwaysCheckNewPosts,
		Guid scheduleId,
		bool deleteText,
		bool deleteMedia,
		string[] avoidWords,
		bool needVerifiedPosts,
		DateTime? dateFrom,
		DateTime? dateTo,
		bool useAiForPosts,
		Guid telegramSessionId,
		CancellationToken ct
	);

	Task<string?> GetTelegramTokenAsync(Guid scheduleId, Guid userId, CancellationToken ct);
}