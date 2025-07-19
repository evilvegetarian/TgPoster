using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.Parse.ListChannel;

internal sealed class ListParseChannelsUseCases(IListParseChannelsStorage storage, IIdentityProvider identity)
    : IRequestHandler<ListParseChannelsQuery, List<ParseChannelsResponse>>
{
    public Task<List<ParseChannelsResponse>> Handle(ListParseChannelsQuery request, CancellationToken ct)
    {
        return storage.GetChannelAsync(identity.Current.UserId, ct);
    }
}