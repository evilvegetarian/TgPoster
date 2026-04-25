using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Proxies.CreateProxy;

public interface ICreateProxyStorage
{
	Task<Guid> CreateAsync(
		Guid userId,
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
