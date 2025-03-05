using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.Services;
using TgPoster.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Mapping;
using ContentTypes = TgPoster.Domain.Services.ContentTypes;

namespace TgPoster.Storage.Storages;

internal class CreateMessagesFromFilesUseCaseStorage(PosterContext context, GuidFactory guidFactory)
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
                ChatId = x.TelegramBot.ChatId,
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<DateTime>> GetExistMessageTimePosting(Guid scheduleId, CancellationToken cancellationToken)
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
        List<DateTime> postingTime,
        CancellationToken cancellationToken
    )
    {
        for (int i = 0; i < files.Count; i++)
        {
            var messageId = guidFactory.New();

            MessageFile messageFile = files[i].Type == ContentTypes.Photo
                ? new PhotoMessageFile
                {
                    Id = guidFactory.New(),
                    MessageId = messageId,
                    TgFileId = files[i].FileId,
                    Type = files[i].Type.ToStorage()
                }
                : new VideoMessageFile
                {
                    Id = guidFactory.New(),
                    MessageId = messageId,
                    TgFileId = files[i].FileId,
                    Type = files[i].Type.ToStorage(),
                    ThumbnailIds = files[i].PreviewPhotoIds
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