using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Discover.ListDiscover;

public sealed record ListDiscoverQuery(
    int Page,
    int PageSize,
    string? Category,
    string? Search,
    string? PeerType) : IRequest<PagedResponse<DiscoverChannelResponse>>;
