using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.BackGround.SenderMessageWorker;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

public class SenderMessageStorage(PosterContext context) : ISenderMessageStorage
{
    public async Task<List<MessageDetail>> GetMessagesAsync()
    {
        var time = DateTimeOffset.UtcNow;
        var plusMinute = time.AddMinutes(1);

        var messages = await context.Schedules
            .Where(x => x.Messages.Any(m => m.TimePosting > time
                                            && m.TimePosting <= plusMinute
                                            && m.Status == MessageStatus.Register))
            .Select(x => new MessageDetail
            {
                ChannelId = x.ChannelId,
                Api = x.TelegramBot.ApiTelegram,
                MessageDto = x.Messages
                    .Where(m => m.TimePosting > time
                                && m.TimePosting <= plusMinute
                                && m.Status == MessageStatus.Register)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        Message = m.TextMessage,
                        TimePosting = m.TimePosting,
                        File = m.MessageFiles.Select(f => new FileDto
                        {
                            TgFileId = f.TgFileId,
                            Caption = f.Caption,
                            ContentType = f.ContentType,
                        }).ToList()
                    }).ToList()
            })
            .ToListAsync();
        var messageIds = messages
            .SelectMany(x => x.MessageDto
                .Select(m => m.Id))
            .ToList();
        if (messageIds.Any())
        {
            await context.Messages
                .Where(m => messageIds.Contains(m.Id))
                .ExecuteUpdateAsync(m => m.SetProperty(msg => msg.Status, MessageStatus.InHandle));
        }

        return messages;
    }

    public async Task UpdateStatusMessage(Guid id)
    {
       await context.Messages
            .Where(m =>m.Id==id)
            .ExecuteUpdateAsync(m => m.SetProperty(msg => msg.Status, MessageStatus.Send));
    }
}