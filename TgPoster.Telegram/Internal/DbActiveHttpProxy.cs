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

	private readonly Lock initGate = new();

	private volatile Snapshot snapshot = Snapshot.Empty;
	private volatile bool initialized;
	private int refreshing;

	/// <summary>
	///     Креды текущего прокси. Управляются снимком из БД, сеттер игнорируется
	/// </summary>
	public ICredentials? Credentials
	{
		get
		{
			EnsureFresh();
			return snapshot.Credentials;
		}
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
		// Первая загрузка — блокирующая: иначе SocketsHttpHandler успевает прочитать пустой снимок
		// (Credentials = null) до завершения фонового обновления и шлёт CONNECT без Proxy-Authorization → 407
		if (!initialized)
		{
			EnsureInitialized();
			return;
		}

		if (DateTimeOffset.UtcNow - snapshot.FetchedAt >= refreshInterval)
		{
			TriggerRefresh();
		}
	}

	private void EnsureInitialized()
	{
		if (initialized)
		{
			return;
		}

		lock (initGate)
		{
			if (initialized)
			{
				return;
			}

			try
			{
				RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Не удалось выполнить первичную загрузку HTTP-прокси для запросов к Telegram");
			}
			finally
			{
				initialized = true;
			}
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

		var previous = snapshot;
		snapshot = Build(proxy);

		var firstLoad = previous.FetchedAt == DateTimeOffset.MinValue;
		var changed = previous.ProxyUri?.ToString() != snapshot.ProxyUri?.ToString();
		if (firstLoad || changed)
		{
			if (snapshot.ProxyUri is null)
			{
				logger.LogWarning("Активный HTTP-прокси в БД не найден — запросы к Telegram идут напрямую");
			}
			else
			{
				// Сам URI прокси не содержит логин/пароль, поэтому отдельно логируем наличие аутентификации —
				// при 407 от прокси это сразу показывает, заполнены ли креды в БД
				logger.LogInformation(
					"Активный HTTP-прокси для запросов к Telegram: {Proxy}. Аутентификация: {AuthMode}",
					snapshot.ProxyUri,
					snapshot.Credentials is null ? "НЕ настроена (логин/пароль пусты)" : "настроена");
			}
		}
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
