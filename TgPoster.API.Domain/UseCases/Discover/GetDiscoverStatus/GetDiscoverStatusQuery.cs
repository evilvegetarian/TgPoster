using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

public sealed record GetDiscoverStatusQuery : IRequest<DiscoverStatusResponse>;