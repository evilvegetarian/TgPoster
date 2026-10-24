namespace TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;

public interface IUpdateParseChannelStorage
{
	Task<bool> ExistParseChannelAsync(Guid id, Guid userId, CancellationToken cancellationToken);
	Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken cancellationToken);
	Task<bool> ExistTelegramSessionAsync(Guid telegramSessionId, Guid userId, CancellationToken cancellationToken);
	Task UpdateParseChannelAsync(UpdateParseChannelCommand command, CancellationToken cancellationToken);
}