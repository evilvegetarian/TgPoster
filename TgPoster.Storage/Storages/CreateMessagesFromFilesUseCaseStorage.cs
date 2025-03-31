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
    public Task<TelegramBotDto?> GetTelegramBot(Guid scheduleId, Guid userId, CancellationToken cancellationToken)
    {
        return context.Schedules
            .Include(x => x.TelegramBot)
            .Where(x => x.Id == scheduleId)
            .Where(x => x.TelegramBot.OwnerId == userId)
            .Select(x => new TelegramBotDto
            {
                ApiTelegram = x.TelegramBot.ApiTelegram,
                ChatId = x.TelegramBot.ChatId
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<DateTimeOffset>> GetExistMessageTimePosting(Guid scheduleId, CancellationToken cancellationToken)
    {
        return context.Messages
            .Where(x => x.ScheduleId == scheduleId)
            .Where(x => x.TimePosting > DateTimeOffset.UtcNow)
            .Where(x => x.Status == MessageStatus.Register)
            .Select(x => x.TimePosting)
            .ToListAsync(cancellationToken);
    }

    public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTime(
        Guid scheduleId,
        CancellationToken cancellationToken
    )
    {
        return context.Days
            .Where(x => x.ScheduleId == scheduleId)
            .ToDictionaryAsync(x => x.DayOfWeek, x => x.TimePostings.ToList(), cancellationToken);
    }

    public async Task CreateMessages(
        Guid scheduleId,
        List<MediaFileResult> files,
        List<DateTimeOffset> postingTime,
        CancellationToken cancellationToken
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
                    ContentType = file.ContentType
                }
                : new VideoMessageFile
                {
                    Id = guidFactory.New(),
                    MessageId = messageId,
                    TgFileId = file.FileId,
                    ContentType = file.ContentType,
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

            await context.Messages.AddAsync(message, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}