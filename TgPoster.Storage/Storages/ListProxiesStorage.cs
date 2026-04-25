using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Proxies.ListProxies;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class ListProxiesStorage(PosterContext context) : IListProxiesStorage
{
	public Task<bool> BelongsToUserAsync(Guid userId, Guid proxyId, CancellationToken ct)
	{
		return context.Proxies.AnyAsync(p => p.Id == proxyId && p.UserId == userId, ct);
	}

	public async Task<List<ProxyResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct)
	{
		var items = await context.Proxies
			.Where(p => p.UserId == userId)
			.OrderByDescending(p => p.Created)
			.Select(p => new
			{
				p.Id,
				p.Name,
				p.Type,
				p.Host,
				p.Port,
				p.Username,
				p.Password,
				p.Secret,
				p.Created,
				SessionsCount = p.Sessions.Count
			})
			.ToListAsync(ct);

		return items.ConvertAll(p => new ProxyResponse(
			p.Id,
			p.Name,
			(ProxyType)p.Type,
			p.Host,
			p.Port,
			p.Username,
			p.Password,
			p.Secret,
			p.SessionsCount,
			p.Created
		));
	}
}
