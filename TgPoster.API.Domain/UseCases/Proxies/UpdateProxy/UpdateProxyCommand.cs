using MediatR;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Proxies.UpdateProxy;

public sealed record UpdateProxyCommand(
	Guid Id,
	string Name,
	ProxyType Type,
	string Host,
	int Port,
	string? Username,
	string? Password,
	string? Secret
) : IRequest;
