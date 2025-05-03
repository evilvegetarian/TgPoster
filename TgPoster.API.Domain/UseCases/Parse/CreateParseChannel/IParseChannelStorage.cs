namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

public interface IParseChannelStorage
{
    Task<Guid> AddParseChannelParameters(
        string channel,
        bool alwaysCheckNewPosts,
        Guid scheduleId,
        bool deleteText,
        bool deleteMedia,
        string[] avoidWords,
        bool needVerifiedPosts,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken
    );

    Task<string?> GetTelegramToken(Guid scheduleId, CancellationToken cancellationToken);
}