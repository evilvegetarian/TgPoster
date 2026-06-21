using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace Shared.Telegram;

/// <summary>
///     Менеджер для кеширования и переиспользования TelegramBotClient экземпляров
/// </summary>
/// <param name="proxy">
///     Прокси для запросов к api.telegram.org. Тот же <see cref="IWebProxy" />, что и для парсинга t.me.
///     Если не зарегистрирован — запросы идут напрямую
/// </param>
/// <param name="logger">Логгер (опционален, чтобы не ломать юнит-тесты)</param>
public sealed class TelegramBotManager(IWebProxy? proxy = null, ILogger<TelegramBotManager>? logger = null)
	: IDisposable
{
	private readonly ConcurrentDictionary<string, TelegramBotClient> clients = [];

	public void Dispose()
	{
		clients.Clear();
	}

	/// <summary>
	///     Получает или создает TelegramBotClient для указанного токена
	/// </summary>
	/// <param name="token">Токен бота Telegram</param>
	/// <returns>Закэшированный или новый экземпляр <see cref="TelegramBotClient" /></returns>
	public TelegramBotClient GetClient(string token)
	{
		return clients.GetOrAdd(token, t =>
		{
			logger?.LogInformation(
				"Создание TelegramBotClient. Прокси для api.telegram.org: {ProxyMode}",
				proxy is null ? "НЕ настроен (запросы напрямую)" : "включён");

			var handler = new SocketsHttpHandler
			{
				PooledConnectionLifetime = TimeSpan.FromMinutes(2)
			};

			// Когда прокси задан — устанавливаем туннель сами и шлём Proxy-Authorization: Basic превентивно.
			// Штатная реактивная авторизация SocketsHttpHandler с динамическим IWebProxy ненадёжна:
			// креды могут быть прочитаны как null до загрузки снимка, и в CONNECT уходит запрос без авторизации → 407
			if (proxy is not null)
			{
				handler.ConnectCallback = ConnectThroughProxyAsync;
			}

			var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
			return new TelegramBotClient(t, http);
		});
	}

	/// <summary>
	///     Удаляет клиента из кеша (например, при удалении бота из БД)
	/// </summary>
	public bool RemoveClient(string token) => clients.TryRemove(token, out _);

	/// <summary>
	///     Устанавливает TCP-соединение для запроса: при наличии активного прокси открывает CONNECT-туннель
	///     с превентивной Basic-авторизацией, иначе идёт напрямую
	/// </summary>
	/// <param name="context">Контекст соединения с целевым хостом (api.telegram.org)</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Поток транспорта, поверх которого SocketsHttpHandler сам поднимет TLS</returns>
	private async ValueTask<Stream> ConnectThroughProxyAsync(SocketsHttpConnectionContext context, CancellationToken ct)
	{
		var target = context.DnsEndPoint;
		var destination = new UriBuilder("https", target.Host, target.Port).Uri;
		var proxyUri = proxy!.IsBypassed(destination) ? null : proxy.GetProxy(destination);

		if (proxyUri is null)
		{
			return await OpenTcpAsync(target.Host, target.Port, ct);
		}

		var stream = await OpenTcpAsync(proxyUri.Host, proxyUri.Port, ct);
		try
		{
			await EstablishTunnelAsync(stream, target.Host, target.Port, proxyUri, ct);
			return stream;
		}
		catch
		{
			await stream.DisposeAsync();
			throw;
		}
	}

	/// <summary>
	///     Отправляет CONNECT прокси-серверу с превентивным заголовком Proxy-Authorization и проверяет ответ
	/// </summary>
	/// <param name="stream">Открытый поток до прокси-сервера</param>
	/// <param name="host">Целевой хост назначения</param>
	/// <param name="port">Целевой порт назначения</param>
	/// <param name="proxyUri">Адрес прокси-сервера</param>
	/// <param name="ct">Токен отмены</param>
	private async ValueTask EstablishTunnelAsync(
		Stream stream,
		string host,
		int port,
		Uri proxyUri,
		CancellationToken ct
	)
	{
		var request = new StringBuilder()
			.Append("CONNECT ").Append(host).Append(':').Append(port).Append(" HTTP/1.1\r\n")
			.Append("Host: ").Append(host).Append(':').Append(port).Append("\r\n");

		var credential = proxy!.Credentials?.GetCredential(proxyUri, "Basic");
		if (credential is not null && !string.IsNullOrEmpty(credential.UserName))
		{
			var raw = Encoding.UTF8.GetBytes($"{credential.UserName}:{credential.Password}");
			request.Append("Proxy-Authorization: Basic ").Append(Convert.ToBase64String(raw)).Append("\r\n");
		}

		request.Append("Proxy-Connection: Keep-Alive\r\n\r\n");

		await stream.WriteAsync(Encoding.ASCII.GetBytes(request.ToString()), ct);
		await stream.FlushAsync(ct);

		var status = await ReadTunnelStatusAsync(stream, ct);
		if (status != HttpStatusCode.OK)
		{
			throw new HttpRequestException(
				$"Прокси {proxyUri.Host}:{proxyUri.Port} отклонил CONNECT к {host}:{port} (HTTP {(int)status})");
		}
	}

	/// <summary>
	///     Читает заголовок ответа прокси на CONNECT побайтово до пустой строки, чтобы не задеть байты TLS-рукопожатия,
	///     и возвращает код статуса
	/// </summary>
	/// <param name="stream">Поток до прокси-сервера</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>HTTP-код из статусной строки ответа</returns>
	private static async ValueTask<HttpStatusCode> ReadTunnelStatusAsync(Stream stream, CancellationToken ct)
	{
		var buffer = new byte[1];
		var header = new StringBuilder();
		var matched = 0;

		while (matched < 4)
		{
			var read = await stream.ReadAsync(buffer, ct);
			if (read == 0)
			{
				throw new HttpRequestException("Прокси закрыл соединение до завершения ответа на CONNECT");
			}

			var symbol = (char)buffer[0];
			header.Append(symbol);
			matched = (matched, symbol) switch
			{
				(0, '\r') => 1,
				(1, '\n') => 2,
				(2, '\r') => 3,
				(3, '\n') => 4,
				_ => symbol == '\r' ? 1 : 0
			};
		}

		var response = header.ToString();
		var lineEnd = response.IndexOf("\r\n", StringComparison.Ordinal);
		var statusLine = lineEnd < 0 ? response : response[..lineEnd];
		var parts = statusLine.Split(' ', 3);
		if (parts.Length >= 2 && int.TryParse(parts[1], out var code))
		{
			return (HttpStatusCode)code;
		}

		throw new HttpRequestException($"Некорректный ответ прокси на CONNECT: {statusLine}");
	}

	private static async ValueTask<Stream> OpenTcpAsync(string host, int port, CancellationToken ct)
	{
		var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
		try
		{
			await socket.ConnectAsync(host, port, ct);
			return new NetworkStream(socket, true);
		}
		catch
		{
			socket.Dispose();
			throw;
		}
	}
}