using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class ListMessageStorage(PosterContext context) : IListMessageStorage
{
    public Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ct)
    {
        return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
    }

    public Task<string?> GetApiTokenAsync(Guid scheduleId, CancellationToken ct)
    {
        return context.Schedules
            .Where(x => x.Id == scheduleId)
            .Select(x => x.TelegramBot.ApiTelegram)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid scheduleId, CancellationToken ct)
    {
        var messages = await context.Messages
            .Where(message => message.ScheduleId == scheduleId)
            .Include(message => message.MessageFiles)
            .ToListAsync(ct);

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