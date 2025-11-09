namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannel;

public interface IListParseChannelsStorage
{
	Task<List<ParseChannelsResponse>> GetChannelParsingParametersAsync(Guid userId, CancellationToken ct);
}