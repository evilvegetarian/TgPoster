using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal class ParseChannelStorage(PosterContext context, GuidFactory factory) : IParseChannelStorage
{
    public async Task<Guid> AddParseChannelParameters(
        string channel,
        bool alwaysCheckNewPosts,
        Guid scheduleId,
        bool deleteText,
        bool deleteMedia,
        string[] avoidWords,
        bool needVerifiedPosts,
        CancellationToken cancellationToken
    )
    {
        var id = factory.New();
        var param = new ChannelParsingParameters
        {
            Id = id,
            ScheduleId = scheduleId,
            AvoidWords = avoidWords,
            DeleteText = deleteText,
            DeleteMedia = deleteMedia,
            CheckNewPosts = alwaysCheckNewPosts,
            NeedVerifiedPosts = needVerifiedPosts,
            Channel = channel,
            Status = ParsingStatus.New
        };
        await context.AddAsync(param, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return id;
    }

    public Task<string?> GetTelegramToken(Guid scheduleId, CancellationToken cancellationToken)
    {
        return context.Schedules.Where(x => x.Id == scheduleId)
            .Select(x => x.TelegramBot.ApiTelegram).FirstOrDefaultAsync(cancellationToken);
    }
}