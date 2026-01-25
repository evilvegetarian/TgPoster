using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannel;

internal sealed class ListParseChannelsUseCase(IListParseChannelsStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListParseChannelsQuery, ParseChannelListResponse>
{
	public async Task<ParseChannelListResponse> Handle(ListParseChannelsQuery request, CancellationToken ct)
	{
		var items = await storage.GetChannelParsingParametersAsync(identity.Current.UserId, ct);
		return new ParseChannelListResponse { Items = items };
	}
}