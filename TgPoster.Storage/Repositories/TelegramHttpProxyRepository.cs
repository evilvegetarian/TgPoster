using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Repositories;

internal sealed class TelegramHttpProxyRepository(PosterContext context) : ITelegramHttpProxyRepository
{
	public Task<ProxyDto?> GetActiveHttpProxyAsync(CancellationToken ct)
	{
		return context.Proxies
			.Where(p => p.Type == Data.Enum.ProxyType.Http)
			.OrderBy(p => p.Created)
			.Select(p => new ProxyDto(
				(ProxyType)p.Type,
				p.Host,
				p.Port,
				p.Username,
				p.Password,
				p.Secret))
			.FirstOrDefaultAsync(ct);
	}
}
