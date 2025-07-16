using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.Parse.ListChannel;

internal sealed class ListParseChannelsUseCases(IListParseChannelsStorage storage, IIdentityProvider identity)
    : IRequestHandler<ListParseChannelsQuery, List<ParseChannelsResponse>>
{
    public async Task<List<ParseChannelsResponse>> Handle(ListParseChannelsQuery request, CancellationToken ct)
    {
        return await storage.GetChannelAsync(identity.Current.UserId, ct);
    }
}