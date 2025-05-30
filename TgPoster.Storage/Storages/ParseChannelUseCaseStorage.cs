using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Storage.Storages;

internal class ParseChannelUseCaseStorage(PosterContext context, GuidFactory guidFactory) : IParseChannelUseCaseStorage
{
    public Task<ParametersDto?> GetChannelParsingParametersAsync(Guid id, CancellationToken ct)
    {
        return context.ChannelParsingParameters
            .Where(x => x.Id == id)
            .Select(ch => new ParametersDto
            {
                Token = ch.Schedule.TelegramBot.ApiTelegram,
                ChatId = ch.Schedule.TelegramBot.ChatId,
                AvoidWords = ch.AvoidWords,
                ChannelName = ch.Channel,
                DeleteMedia = ch.DeleteMedia,
                DeleteText = ch.DeleteText,
                FromDate = ch.DateFrom,
                LastParsedId = ch.LastParseId,
                ToDate = ch.DateTo,
                IsNeedVerified = ch.NeedVerifiedPosts,
                ScheduleId = ch.ScheduleId,
                CheckNewPosts = ch.CheckNewPosts
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task CreateMessagesAsync(List<MessageDto> messages, CancellationToken ct)
    {
        var message = messages.Select(x =>
        {
            var id = guidFactory.New();
            return new Message
            {
                Id = id,
                TextMessage = x.Text,
                ScheduleId = x.ScheduleId,
                IsVerified = !x.IsNeedVerified,
                Status = MessageStatus.Register,
                TimePosting = x.TimePosting,
                IsTextMessage = x.Text.IsTextMessage(),
                MessageFiles = x.Media.Select<MediaDto, MessageFile>(m =>
                {
                    if (m.PreviewPhotoIds.Count != 0)
                    {
                        return new VideoMessageFile
                        {
                            Id = guidFactory.New(),
                            MessageId = id,
                            ContentType = m.MimeType,
                            TgFileId = m.FileId,
                            ThumbnailIds = m.PreviewPhotoIds
                        };
                    }

                    return new PhotoMessageFile
                    {
                        Id = guidFactory.New(),
                        ContentType = m.MimeType,
                        TgFileId = m.FileId,
                        MessageId = id
                    };
                }).ToList()
            };
        });

        context.Messages.AddRangeAsync(message, ct);
        return context.SaveChangesAsync(ct);
    }

    public Task UpdateChannelParsingParametersAsync(Guid id, int offsetId, bool checkNewPosts, CancellationToken ct)
    {
        var status = checkNewPosts
            ? ParsingStatus.Waiting
            : ParsingStatus.Finished;
        return context.ChannelParsingParameters
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(updater =>
                    updater
                        .SetProperty(x => x.Status, status)
                        .SetProperty(x => x.LastParseId, offsetId),
                ct);
    }

    public Task UpdateInHandleStatusAsync(Guid id, CancellationToken ct)
    {
        return context.ChannelParsingParameters
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(updater =>
                    updater
                        .SetProperty(x => x.Status, ParsingStatus.InHandle),
                ct);
    }

    public Task UpdateErrorStatusAsync(Guid id, CancellationToken ct)
    {
        return context.ChannelParsingParameters
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(updater =>
                    updater
                        .SetProperty(x => x.Status, ParsingStatus.Failed),
                ct);
    }

    public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct)
    {
        return context.Days
            .Where(x => x.ScheduleId == scheduleId)
            .ToDictionaryAsync(x => x.DayOfWeek, x => x.TimePostings.OrderBy(time => time).ToList(), ct);
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
}