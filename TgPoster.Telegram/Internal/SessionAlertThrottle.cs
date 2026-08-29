using System.Collections.Concurrent;
using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Internal;

/// <summary>
///     Ограничивает частоту оповещений: одна и та же проблема по одной сессии уходит владельцу
///     не чаще, чем раз в период остывания. Иначе при массовых операциях (импорт каналов, рассылка)
///     владелец получит сотни одниаковых сообщений
/// </summary>
internal sealed class SessionAlertThrottle
{
	private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);

	private readonly ConcurrentDictionary<(Guid SessionId, TelegramSessionProblem Problem), DateTimeOffset> lastSent =
		[];

	/// <summary>
	///     Проверяет, прошло ли достаточно времени с прошлого оповещения, и отмечает текущую отправку
	/// </summary>
	/// <param name="sessionId">Id Telegram сессии</param>
	/// <param name="problem">Тип проблемы</param>
	/// <returns>true, если оповещение можно отправлять</returns>
	public bool TryAcquire(Guid sessionId, TelegramSessionProblem problem)
	{
		var now = DateTimeOffset.UtcNow;
		var key = (sessionId, problem);

		while (true)
		{
			if (!lastSent.TryGetValue(key, out var previous))
			{
				if (lastSent.TryAdd(key, now))
				{
					return true;
				}

				continue;
			}

			if (now - previous < Cooldown)
			{
				return false;
			}

			if (lastSent.TryUpdate(key, now, previous))
			{
				return true;
			}
		}
	}
}
