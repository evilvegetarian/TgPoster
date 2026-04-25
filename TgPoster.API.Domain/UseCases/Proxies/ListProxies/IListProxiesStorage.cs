namespace TgPoster.API.Domain.UseCases.Proxies.ListProxies;

public interface IListProxiesStorage
{
	Task<List<ProxyResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct);
	Task<bool> BelongsToUserAsync(Guid userId, Guid proxyId, CancellationToken ct);
}
