using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Proxies.UpdateProxy;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateProxyStorage(PosterContext context) : IUpdateProxyStorage
{
	public Task<bool> ExistsAsync(Guid userId, Guid proxyId, CancellationToken ct)
	{
		return context.Proxies.AnyAsync(p => p.Id == proxyId && p.UserId == userId, ct);
	}

	public async Task<List<Guid>> UpdateAsync(
		Guid proxyId,
		string name,
		ProxyType type,
		string host,
		int port,
		string? username,
		string? password,
		string? secret,
		CancellationToken ct)
	{
		var proxy = await context.Proxies.FirstAsync(p => p.Id == proxyId, ct);

		proxy.Name = name;
		proxy.Type = (Data.Enum.ProxyType)type;
		proxy.Host = host;
		proxy.Port = port;
		proxy.Username = username;
		proxy.Password = password;
		proxy.Secret = secret;

		var affectedSessionIds = await context.TelegramSessions
			.Where(s => s.ProxyId == proxyId)
			.Select(s => s.Id)
			.ToListAsync(ct);

		await context.SaveChangesAsync(ct);
		return affectedSessionIds;
	}
}
