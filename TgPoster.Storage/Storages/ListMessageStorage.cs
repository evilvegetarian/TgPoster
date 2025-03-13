using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class ListMessageStorage(PosterContext context) : IListMessageStorage
{
    public Task<bool> ExistSchedule(Guid scheduleId, CancellationToken cancellationToken)
    {
        return context.Schedules.AnyAsync(x => x.Id == scheduleId, cancellationToken);
    }

    public Task<string?> GetApiToken(Guid scheduleId, CancellationToken cancellationToken)
    {
        return context.Schedules
            .Where(x => x.Id == scheduleId)
            .Select(x => x.TelegramBot.ApiTelegram)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var messages = await context.Messages
            .Where(message => message.ScheduleId == scheduleId)
            .Include(message => message.MessageFiles)
            .ToListAsync(cancellationToken);

        return messages.Select(message => new MessageDto
        {
            Id = message.Id,
            TextMessage = message.TextMessage,
            ScheduleId = message.ScheduleId,
            TimePosting = message.TimePosting,
            Files = message.MessageFiles.Select(file => new FileDto
            {
                Id = file.Id,
                ContentType = file.ContentType,
                TgFileId = file.TgFileId,
                PreviewIds = file is VideoMessageFile videoFile
                    ? videoFile.ThumbnailIds.ToList()
                    : []
            }).ToList()
        }).ToList();
    }
}