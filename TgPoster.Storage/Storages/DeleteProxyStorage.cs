using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Proxies.DeleteProxy;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class DeleteProxyStorage(PosterContext context) : IDeleteProxyStorage
{
	public Task<bool> ExistsAsync(Guid userId, Guid proxyId, CancellationToken ct)
	{
		return context.Proxies.AnyAsync(p => p.Id == proxyId && p.UserId == userId, ct);
	}

	public async Task<List<Guid>> DeleteAsync(Guid proxyId, CancellationToken ct)
	{
		var proxy = await context.Proxies.FirstAsync(p => p.Id == proxyId, ct);

		var affectedSessionIds = await context.TelegramSessions
			.Where(s => s.ProxyId == proxyId)
			.Select(s => s.Id)
			.ToListAsync(ct);

		foreach (var sessionId in affectedSessionIds)
		{
			var session = await context.TelegramSessions.FirstAsync(s => s.Id == sessionId, ct);
			session.ProxyId = null;
		}

		context.Proxies.Remove(proxy);
		await context.SaveChangesAsync(ct);
		return affectedSessionIds;
	}
}
