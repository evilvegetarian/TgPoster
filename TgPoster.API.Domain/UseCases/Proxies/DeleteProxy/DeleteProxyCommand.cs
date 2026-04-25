using MediatR;

namespace TgPoster.API.Domain.UseCases.Proxies.DeleteProxy;

public sealed record DeleteProxyCommand(Guid Id) : IRequest;
