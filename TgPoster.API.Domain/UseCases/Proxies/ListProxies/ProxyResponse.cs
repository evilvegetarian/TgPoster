using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Proxies.ListProxies;

public sealed record ProxyResponse(
	Guid Id,
	string Name,
	ProxyType Type,
	string Host,
	int Port,
	string? Username,
	string? Password,
	string? Secret,
	int SessionsCount,
	DateTimeOffset? Created
);

public sealed record ProxyListResponse
{
	public required List<ProxyResponse> Items { get; init; }
}
