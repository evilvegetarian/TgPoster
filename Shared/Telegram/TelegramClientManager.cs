using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WTelegram;

namespace Shared.Telegram;

/// <summary>
///     Менеджер для управления активными и ожидающими WTelegram клиентами.
/// </summary>
public sealed class TelegramClientManager(ILogger<TelegramClientManager> logger) : IDisposable
{
	private readonly ConcurrentDictionary<Guid, Client> activeClients = [];
	private readonly ConcurrentDictionary<Guid, Client> pendingClients = [];

	public void Dispose()
	{
		foreach (var client in pendingClients.Values)
		{
			client.Dispose();
		}

		pendingClients.Clear();

		foreach (var client in activeClients.Values)
		{
			client.Dispose();
		}

		activeClients.Clear();

		logger.LogInformation("Все Telegram клиенты освобождены");
	}

	/// <summary>
	///     Получает активного клиента для указанной сессии, если он существует и подключен.
	/// </summary>
	public bool TryGetActiveClient(Guid sessionId, out Client? client)
	{
		if (activeClients.TryGetValue(sessionId, out var existingClient) && !existingClient.Disconnected)
		{
			client = existingClient;
			return true;
		}

		client = null;
		return false;
	}

	/// <summary>
	///     Добавляет клиента в кеш активных клиентов.
	/// </summary>
	public void AddActiveClient(Guid sessionId, Client client)
	{
		activeClients[sessionId] = client;
		logger.LogDebug("Клиент добавлен в активные для сессии {SessionId}", sessionId);
	}

	/// <summary>
	///     Удаляет активного клиента и освобождает ресурсы.
	/// </summary>
	public async Task RemoveActiveClientAsync(Guid sessionId)
	{
		if (activeClients.TryRemove(sessionId, out var client))
		{
			await client.DisposeAsync();
			logger.LogInformation("Клиент для сессии {SessionId} удален из активных", sessionId);
		}
	}

	/// <summary>
	///     Получает ожидающего клиента для указанной сессии.
	/// </summary>
	public bool TryGetPendingClient(Guid sessionId, out Client? client) =>
		pendingClients.TryGetValue(sessionId, out client);

	/// <summary>
	///     Добавляет клиента в кеш ожидающих клиентов.
	/// </summary>
	public void AddPendingClient(Guid sessionId, Client client)
	{
		pendingClients.TryAdd(sessionId, client);
		logger.LogDebug("Клиент добавлен в ожидающие для сессии {SessionId}", sessionId);
	}

	/// <summary>
	///     Удаляет ожидающего клиента без освобождения ресурсов (клиент может стать активным).
	/// </summary>
	public bool RemovePendingClient(Guid sessionId)
	{
		var removed = pendingClients.TryRemove(sessionId, out _);
		if (removed)
		{
			logger.LogDebug("Клиент удален из ожидающих для сессии {SessionId}", sessionId);
		}

		return removed;
	}

	/// <summary>
	///     Удаляет ожидающего клиента и освобождает ресурсы.
	/// </summary>
	public async Task RemovePendingClientWithDisposeAsync(Guid sessionId)
	{
		if (pendingClients.TryRemove(sessionId, out var client))
		{
			await client.DisposeAsync();
			logger.LogInformation("Клиент для сессии {SessionId} удален из ожидающих с освобождением ресурсов",
				sessionId);
		}
	}
}