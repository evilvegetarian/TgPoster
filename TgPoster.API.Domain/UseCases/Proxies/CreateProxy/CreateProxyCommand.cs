using MediatR;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Proxies.CreateProxy;

public sealed record CreateProxyCommand(
	string Name,
	ProxyType Type,
	string Host,
	int Port,
	string? Username,
	string? Password,
	string? Secret
) : IRequest<CreateProxyResponse>;
