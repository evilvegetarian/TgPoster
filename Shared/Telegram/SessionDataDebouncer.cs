using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Telegram;

/// <summary>
///     Debouncer для сохранения данных Telegram сессий в БД.
///     Накапливает последнее значение и записывает после паузы, но не реже чем раз в MaxDelay.
/// </summary>
public sealed class SessionDataDebouncer(
	IServiceScopeFactory scopeFactory,
	ILogger<SessionDataDebouncer> logger) : IAsyncDisposable
{
	private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

	private readonly ConcurrentDictionary<Guid, SessionEntry> entries = [];

	public async ValueTask DisposeAsync()
	{
		foreach (var entry in entries.Values)
		{
			await entry.DebounceTimer.DisposeAsync();
			await entry.MaxDelayTimer.DisposeAsync();
		}

		await FlushAllAsync();
		entries.Clear();
	}

	/// <summary>
	///     Запоминает данные сессии и сбрасывает таймер debounce.
	///     Гарантирует запись в БД не реже чем раз в 30 секунд.
	/// </summary>
	public void Update(Guid sessionId, byte[] sessionData)
	{
		var entry = entries.GetOrAdd(sessionId, id =>
		{
			var e = new SessionEntry(
				new Timer(_ => _ = FlushAsync(id), null, Timeout.Infinite, Timeout.Infinite),
				new Timer(_ => _ = FlushAsync(id), null, Timeout.Infinite, Timeout.Infinite)
			);
			e.MaxDelayTimer.Change(MaxDelay, Timeout.InfiniteTimeSpan);
			return e;
		});

		entry.LatestData = sessionData;
		entry.DebounceTimer.Change(DebounceDelay, Timeout.InfiniteTimeSpan);
	}

	private async Task FlushAsync(Guid sessionId)
	{
		if (!entries.TryGetValue(sessionId, out var entry))
			return;

		var data = Interlocked.Exchange(ref entry.LatestData, null);
		if (data == null)
			return;

		entry.DebounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
		entry.MaxDelayTimer.Change(MaxDelay, Timeout.InfiniteTimeSpan);

		try
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			var repository = scope.ServiceProvider.GetRequiredService<ITelegramAuthRepository>();
			var sessionString = Convert.ToBase64String(data);
			await repository.UpdateSessionDataAsync(sessionId, sessionString, CancellationToken.None);

			logger.LogDebug("Данные сессии {SessionId} сохранены в БД (debounced)", sessionId);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при сохранении данных сессии {SessionId}", sessionId);
		}
	}

	private async Task FlushAllAsync()
	{
		foreach (var sessionId in entries.Keys)
		{
			await FlushAsync(sessionId);
		}
	}

	private sealed class SessionEntry(Timer debounceTimer, Timer maxDelayTimer)
	{
		public Timer DebounceTimer { get; } = debounceTimer;
		public Timer MaxDelayTimer { get; } = maxDelayTimer;
		public byte[]? LatestData;
	}
}
