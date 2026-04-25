using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Proxies.UpdateProxy;

public interface IUpdateProxyStorage
{
	Task<bool> ExistsAsync(Guid userId, Guid proxyId, CancellationToken ct);

	Task<List<Guid>> UpdateAsync(
		Guid proxyId,
		string name,
		ProxyType type,
		string host,
		int port,
		string? username,
		string? password,
		string? secret,
		CancellationToken ct
	);
}
