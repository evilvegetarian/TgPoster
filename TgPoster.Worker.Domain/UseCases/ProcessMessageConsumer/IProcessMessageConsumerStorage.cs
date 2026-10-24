using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;

public interface IProcessMessageConsumerStorage
{
	Task<ParametersDto?> GetChannelParsingParametersAsync(Guid id, CancellationToken ct);
	Task CreateMessageAsync(MessageDto messageDto, CancellationToken ct);

	/// <summary>
	/// Пост из этого сообщения источника уже создан, в том числе если его потом удалили
	/// </summary>
	/// <param name="channelParsingSettingId"></param>
	/// <param name="sourceMessageId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> IsSourceMessageParsedAsync(Guid channelParsingSettingId, int sourceMessageId, CancellationToken ct);
	Task<DateTimeOffset> GeLastMessageTimePostingAsync(Guid scheduleId, CancellationToken ct);
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);
}