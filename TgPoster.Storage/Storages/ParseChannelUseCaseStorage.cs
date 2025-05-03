using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Storage.Storages;

internal class ParseChannelUseCaseStorage(PosterContext context, GuidFactory guidFactory) : IParseChannelUseCaseStorage
{
    public Task<Parameters?> GetChannelParsingParameters(Guid id, CancellationToken cancellationToken)
    {
        return context.ChannelParsingParameters
            .Where(x => x.Id == id)
            .Select(ch => new Parameters
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
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task CreateMessages(List<MessageDto> messages, CancellationToken cancellationToken)
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
                TimePosting = DateTimeOffset.MinValue,
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

        context.Messages.AddRangeAsync(message, cancellationToken);
        return context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateChannelParsingParameters(Guid id, int offsetId, CancellationToken cancellationToken)
    {
        return context.ChannelParsingParameters
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(updater =>
                    updater
                        .SetProperty(x => x.Status, ParsingStatus.Waiting)
                        .SetProperty(x => x.LastParseId, offsetId),
                cancellationToken);
    }

    public Task UpdateInHandleStatus(Guid id, CancellationToken cancellationToken)
    {
        return context.ChannelParsingParameters
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(updater =>
                    updater
                        .SetProperty(x => x.Status, ParsingStatus.InHandle),
                cancellationToken);
    }
}