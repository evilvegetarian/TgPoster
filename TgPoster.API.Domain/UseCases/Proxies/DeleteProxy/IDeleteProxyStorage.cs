namespace TgPoster.API.Domain.UseCases.Proxies.DeleteProxy;

public interface IDeleteProxyStorage
{
	Task<bool> ExistsAsync(Guid userId, Guid proxyId, CancellationToken ct);
	Task<List<Guid>> DeleteAsync(Guid proxyId, CancellationToken ct);
}
