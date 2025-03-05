using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class ListMessageStorage(PosterContext context) : IListMessageStorage
{
    public Task<bool> ExistSchedule(Guid scheduleId, CancellationToken cancellationToken)
    {
        return context.Schedules.AnyAsync(x => x.Id == scheduleId, cancellationToken: cancellationToken);
    }

    public Task<string?> GetApiToken(Guid scheduleId, CancellationToken cancellationToken)
    {
        return context.Schedules
            .Where(x => x.Id == scheduleId)
            .Select(x => x.TelegramBot.ApiTelegram)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public Task<List<MessageDto>> GetMessagesAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        return context.Messages.Where(x => x.ScheduleId == scheduleId)
            .Select(x => new MessageDto
            {
                Id = x.Id,
                TextMessage = x.TextMessage
            }).ToListAsync(cancellationToken: cancellationToken);
    }
}