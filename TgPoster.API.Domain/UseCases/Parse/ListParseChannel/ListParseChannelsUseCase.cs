using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannel;

internal sealed class ListParseChannelsUseCase(IListParseChannelsStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListParseChannelsQuery, List<ParseChannelsResponse>>
{
	public Task<List<ParseChannelsResponse>> Handle(ListParseChannelsQuery request, CancellationToken ct) =>
		storage.GetChannelParsingParametersAsync(identity.Current.UserId, ct);
}