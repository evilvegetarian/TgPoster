using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Mapping;

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

    public async Task<List<MessageDto>> GetMessagesAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var messages = await context.Messages
            .Where(message => message.ScheduleId == scheduleId)
            .Include(message => message.MessageFiles)
            .ToListAsync(cancellationToken);

        return messages.Select(x => new MessageDto
        {
            Id = x.Id,
            TextMessage = x.TextMessage,
            Files = x.MessageFiles.Select(file => new FileDto
            {
                Id = file.Id,
                Type = file.Type.ToDomain(),
                TgFileId = file.TgFileId,
                PreviewIds = file is VideoMessageFile videoFile
                    ? videoFile.ThumbnailIds.ToList()
                    : []
            }).ToList()
        }).ToList();
    }
}