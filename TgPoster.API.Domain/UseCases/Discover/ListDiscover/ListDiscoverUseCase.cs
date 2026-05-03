using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Discover.ListDiscover;

internal sealed class ListDiscoverUseCase(IListDiscoverStorage storage)
    : IRequestHandler<ListDiscoverQuery, PagedResponse<DiscoverChannelResponse>>
{
    public async Task<PagedResponse<DiscoverChannelResponse>> Handle(
        ListDiscoverQuery request, CancellationToken ct)
    {
        var result = await storage.GetDiscoverChannelsAsync(request, ct);
        return new PagedResponse<DiscoverChannelResponse>(result.Items, result.TotalCount, request.Page, request.PageSize);
    }
}
