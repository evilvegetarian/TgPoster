using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class CreateMessagesFromFilesUseCaseStorage(PosterContext context, GuidFactory guidFactory)
    : ICreateMessagesFromFilesUseCaseStorage
{
    public Task<TelegramBotDto?> GetTelegramBotAsync(Guid scheduleId, Guid userId, CancellationToken ct)
    {
        return context.Schedules
            .Include(x => x.TelegramBot)
            .Where(x => x.Id == scheduleId)
            .Where(x => x.TelegramBot.OwnerId == userId)
            .Select(x => new TelegramBotDto
            {
                ApiTelegram = x.TelegramBot.ApiTelegram,
                ChatId = x.TelegramBot.ChatId
            }).FirstOrDefaultAsync(ct);
    }

    public Task<List<DateTimeOffset>> GetExistMessageTimePostingAsync(Guid scheduleId, CancellationToken ct)
    {
        return context.Messages
            .Where(x => x.ScheduleId == scheduleId)
            .Where(x => x.TimePosting > DateTimeOffset.UtcNow)
            .Where(x => x.Status == MessageStatus.Register)
            .Select(x => x.TimePosting)
            .ToListAsync(ct);
    }

    public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct)
    {
        return context.Days
            .Where(x => x.ScheduleId == scheduleId)
            .ToDictionaryAsync(x => x.DayOfWeek, x => x.TimePostings.ToList(), ct);
    }

    public async Task CreateMessagesAsync(
        Guid scheduleId,
        List<MediaFileResult> files,
        List<DateTimeOffset> postingTime,
        CancellationToken ct
    )
    {
        for (var i = 0; i < files.Count; i++)
        {
            var messageId = guidFactory.New();
            var file = files[i];

            MessageFile messageFile = !file.PreviewPhotoIds.Any()
                ? new PhotoMessageFile
                {
                    Id = guidFactory.New(),
                    MessageId = messageId,
                    TgFileId = file.FileId,
                    ContentType = file.MimeType
                }
                : new VideoMessageFile
                {
                    Id = guidFactory.New(),
                    MessageId = messageId,
                    TgFileId = file.FileId,
                    ContentType = file.MimeType,
                    ThumbnailIds = file.PreviewPhotoIds
                };
            var message = new Message
            {
                Id = messageId,
                ScheduleId = scheduleId,
                Status = MessageStatus.Register,
                TimePosting = postingTime[i],
                IsTextMessage = false,
                MessageFiles = [messageFile]
            };

            await context.Messages.AddAsync(message, ct);
        }

        await context.SaveChangesAsync(ct);
    }
}