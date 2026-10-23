using System.Collections.Concurrent;

namespace Shared.Alerts;

/// <summary>
/// Ограничивает частоту оповещений: одна и та же проблема по одному объекту (сессии, расписанию) уходит
/// владельцу не чаще, чем раз в период остывания. Иначе при массовых операциях владелец получит сотни
/// одинаковых сообщений
/// </summary>
public sealed class AlertThrottle(TimeProvider timeProvider)
{
	private readonly ConcurrentDictionary<(Guid SubjectId, Enum Problem), DateTimeOffset> lastSent = [];

	/// <summary>
	/// Проверяет, прошло ли достаточно времени с прошлого оповещения, и отмечает текущую отправку
	/// </summary>
	/// <param name="subjectId"></param>
	/// <param name="problem"></param>
	/// <param name="cooldown"></param>
	/// <returns></returns>
	public bool TryAcquire(Guid subjectId, Enum problem, TimeSpan cooldown)
	{
		var now = timeProvider.GetUtcNow();
		var key = (subjectId, problem);

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

			if (now - previous < cooldown)
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
