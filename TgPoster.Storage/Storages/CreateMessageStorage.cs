using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessage;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateMessageStorage(PosterContext context, GuidFactory guidFactory) : ICreateMessageStorage
{
    public Task<bool> ExistSchedule(Guid userId, Guid scheduleId, CancellationToken ct) =>
        context.Schedules.AnyAsync(x => x.UserId == userId && x.Id == scheduleId, ct);

    public Task<TelegramBotDto?> GetTelegramBot(Guid scheduleId, Guid userId, CancellationToken ct) =>
        context.Schedules
            .Include(x => x.TelegramBot)
            .Where(x => x.UserId == userId && x.Id == scheduleId)
            .Select(x => new TelegramBotDto
            {
                ApiTelegram = x.TelegramBot.ApiTelegram,
                ChatId = x.TelegramBot.ChatId
            }).FirstOrDefaultAsync(ct);

    public async Task<Guid> CreateMessages(
        Guid scheduleId,
        string? text,
        DateTimeOffset time,
        List<MediaFileResult> files,
        CancellationToken ct
    )
    {
        var messageId = guidFactory.New();

        var message = new Message
        {
            Id = messageId,
            ScheduleId = scheduleId,
            TimePosting = time,
            TextMessage = text,
            IsTextMessage = text.IsTextMessage()
        };
        var messageFiles = files.Select(file => new MessageFile
        {
            Id = guidFactory.New(),
            ContentType = file.ContentType,
            MessageId = messageId,
            TgFileId = file.FileId
        });
        await context.Messages.AddAsync(message, ct);
        await context.MessageFiles.AddRangeAsync(messageFiles, ct);
        await context.SaveChangesAsync(ct);
        return messageId;
    }
}