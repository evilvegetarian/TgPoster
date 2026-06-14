using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Repositories;

internal sealed class TelegramHttpProxyRepository(PosterContext context) : ITelegramHttpProxyRepository
{
	public async Task<ProxyDto?> GetActiveHttpProxyAsync(CancellationToken ct)
	{
		// Тип проецируем как есть (Data.Enum.ProxyType со строковым конвертером) и кастим в памяти —
		// иначе кросс-enum каст в SQL превращается в CAST("Type" AS integer) и падает на строковой колонке
		// Берём самую свежую добавленную HTTP-прокси: при замене прокси старые записи не должны перехватывать трафик
		var proxy = await context.Proxies
			.Where(p => p.Type == Data.Enum.ProxyType.Http)
			.OrderByDescending(p => p.Created)
			.Select(p => new
			{
				p.Type,
				p.Host,
				p.Port,
				p.Username,
				p.Password,
				p.Secret
			})
			.FirstOrDefaultAsync(ct);

		return proxy is null
			? null
			: new ProxyDto(
				(ProxyType)proxy.Type,
				proxy.Host,
				proxy.Port,
				proxy.Username,
				proxy.Password,
				proxy.Secret);
	}
}
