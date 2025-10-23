namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public interface IParseChannelUseCaseStorage
{
	Task<ParametersDto?> GetChannelParsingParametersAsync(Guid id, CancellationToken ct);
	Task CreateMessagesAsync(List<MessageDto> messages, CancellationToken ct);
	Task UpdateChannelParsingParametersAsync(Guid id, int offsetId, bool checkNewPosts, CancellationToken ct);
	Task UpdateInHandleStatusAsync(Guid id, CancellationToken ct);
	Task UpdateErrorStatusAsync(Guid id, CancellationToken ct);
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);
	Task<List<DateTimeOffset>> GetExistMessageTimePostingAsync(Guid scheduleId, CancellationToken ct);
}