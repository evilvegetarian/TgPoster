using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Configuration;
using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Internal;

/// <summary>
///     Динамический <see cref="IWebProxy"/> для запросов к t.me: берёт первый активный HTTP-прокси
///     из БД и кэширует его на <see cref="TelegramPublicLookupOptions.ProxyRefreshSeconds"/>.
///     Если активного прокси нет — запросы идут напрямую.
/// </summary>
internal sealed class DbActiveHttpProxy(
	IServiceScopeFactory scopeFactory,
	IOptions<TelegramPublicLookupOptions> options,
	ILogger<DbActiveHttpProxy> logger) : IWebProxy
{
	private readonly TimeSpan refreshInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.ProxyRefreshSeconds));

	private volatile Snapshot snapshot = Snapshot.Empty;
	private int refreshing;

	/// <summary>
	///     Креды текущего прокси. Управляются снимком из БД, сеттер игнорируется
	/// </summary>
	public ICredentials? Credentials
	{
		get => snapshot.Credentials;
		set { }
	}

	public Uri? GetProxy(Uri destination)
	{
		EnsureFresh();
		return snapshot.ProxyUri;
	}

	public bool IsBypassed(Uri host)
	{
		EnsureFresh();
		return snapshot.ProxyUri is null;
	}

	private void EnsureFresh()
	{
		if (DateTimeOffset.UtcNow - snapshot.FetchedAt >= refreshInterval)
		{
			TriggerRefresh();
		}
	}

	private void TriggerRefresh()
	{
		if (Interlocked.Exchange(ref refreshing, 1) == 1)
		{
			return;
		}

		_ = Task.Run(async () =>
		{
			try
			{
				await RefreshAsync(CancellationToken.None);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Не удалось обновить активный HTTP-прокси для t.me");
			}
			finally
			{
				Interlocked.Exchange(ref refreshing, 0);
			}
		});
	}

	private async Task RefreshAsync(CancellationToken ct)
	{
		using var scope = scopeFactory.CreateScope();
		var repository = scope.ServiceProvider.GetRequiredService<ITelegramHttpProxyRepository>();
		var proxy = await repository.GetActiveHttpProxyAsync(ct);

		snapshot = Build(proxy);
	}

	private static Snapshot Build(ProxyDto? proxy)
	{
		if (proxy is null)
		{
			return new Snapshot(null, null, DateTimeOffset.UtcNow);
		}

		var uri = new Uri($"http://{proxy.Host}:{proxy.Port}");
		ICredentials? credentials = string.IsNullOrWhiteSpace(proxy.Username)
			? null
			: new NetworkCredential(proxy.Username, proxy.Password);

		return new Snapshot(uri, credentials, DateTimeOffset.UtcNow);
	}

	private sealed record Snapshot(Uri? ProxyUri, ICredentials? Credentials, DateTimeOffset FetchedAt)
	{
		public static readonly Snapshot Empty = new(null, null, DateTimeOffset.MinValue);
	}
}
