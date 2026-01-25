namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannel;

public interface IListParseChannelsStorage
{
	Task<List<ParseChannelResponse>> GetChannelParsingParametersAsync(Guid userId, CancellationToken ct);
}