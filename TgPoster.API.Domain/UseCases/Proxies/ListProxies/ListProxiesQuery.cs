using MediatR;

namespace TgPoster.API.Domain.UseCases.Proxies.ListProxies;

public sealed record ListProxiesQuery : IRequest<ProxyListResponse>;
