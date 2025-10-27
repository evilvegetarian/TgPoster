using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;

public interface IProcessMessageConsumerStorage
{
	Task<ParametersDto?> GetChannelParsingParametersAsync(Guid id, CancellationToken ct);
	Task CreateMessagesAsync(List<MessageDto> messageDtos, CancellationToken ct);
	Task CreateMessageAsync(MessageDto messageDto, CancellationToken ct);
	Task<List<DateTimeOffset>> GetExistMessageTimePostingAsync(Guid scheduleId, CancellationToken ct);
	Task<DateTimeOffset> GeLastMessageTimePostingAsync(Guid scheduleId, CancellationToken ct);
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);
}