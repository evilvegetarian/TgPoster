namespace TgPoster.API.Domain.UseCases.Parse.ListChannel;

public interface IListParseChannelsStorage
{
    Task<List<ParseChannelsResponse>> GetChannelAsync(Guid userId, CancellationToken ct);
}