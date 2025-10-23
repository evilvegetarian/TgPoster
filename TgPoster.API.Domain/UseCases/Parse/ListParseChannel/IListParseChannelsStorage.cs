namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannel;

public interface IListParseChannelsStorage
{
	Task<List<ParseChannelsResponse>> GetChannelAsync(Guid userId, CancellationToken ct);
}