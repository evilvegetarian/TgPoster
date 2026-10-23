using Microsoft.Extensions.Logging;
using Security.Cryptography;
using Shared.Alerts;
using Shared.Telegram;
using Telegram.Bot;
using TgPoster.Worker.Domain.ConfigModels;

namespace TgPoster.Worker.Domain.Services.ScheduleAlerts;

/// <summary>
/// Отправляет владельцу расписания оповещения о проблемах от имени бота расписания
/// </summary>
internal sealed class ScheduleAlertService(
	ILogger<ScheduleAlertService> logger,
	IScheduleAlertStorage storage,
	AlertThrottle throttle,
	ICryptoAES crypto,
	TelegramOptions options,
	TelegramBotManager botManager) : IScheduleAlertService
{
	private const int DetailsMaxLength = 500;

	public async Task NotifyAsync(
		Guid scheduleId,
		ScheduleProblem problem,
		string details,
		Guid? throttleKey = null,
		CancellationToken ct = default
	)
	{
		try
		{
			if (!throttle.TryAcquire(throttleKey ?? scheduleId, problem, GetCooldown(problem)))
			{
				logger.LogDebug(
					"Оповещение о проблеме {Problem} с расписанием {ScheduleId} пропущено: недавно уже отправляли",
					problem, scheduleId);
				return;
			}

			var target = await storage.GetAlertTargetAsync(scheduleId, ct);
			if (target is null)
			{
				logger.LogDebug(
					"Оповещение о проблеме {Problem} с расписанием {ScheduleId} не отправлено: расписание не найдено",
					problem, scheduleId);
				return;
			}

			var token = crypto.Decrypt(options.SecretKey, target.EncryptedBotToken);
			var bot = botManager.GetClient(token);
			await bot.SendMessage(target.OwnerChatId, BuildMessage(target, problem, details), cancellationToken: ct);

			logger.LogInformation("Отправлено оповещение о проблеме {Problem} с расписанием {ScheduleId}",
				problem, scheduleId);
		}
		catch (Exception ex)
		{
			// Сбой оповещения не должен ломать основной сценарий
			logger.LogError(ex, "Не удалось отправить оповещение о проблеме {Problem} с расписанием {ScheduleId}",
				problem, scheduleId);
		}
	}

	/// <summary>
	/// Собирает текст оповещения для владельца расписания. Текст уходит без parse mode: в подробностях
	/// бывает текст поста, и его разметка не должна ломать отправку
	/// </summary>
	/// <param name="target"></param>
	/// <param name="problem"></param>
	/// <param name="details"></param>
	/// <returns></returns>
	internal static string BuildMessage(ScheduleAlertTarget target, ScheduleProblem problem, string details)
	{
		List<string> lines =
		[
			$"⚠️ Расписание «{target.ScheduleName}» ({target.ChannelName})",
			Describe(problem)
		];

		if (!string.IsNullOrWhiteSpace(details))
		{
			lines.Add(Truncate(details, DetailsMaxLength));
		}

		return string.Join('\n', lines);
	}

	/// <summary>
	/// Обрезает текст до заданой длины вместе с многоточием, не разрывая суррогатную пару (эмодзи)
	/// </summary>
	/// <param name="text"></param>
	/// <param name="maxLength"></param>
	/// <returns></returns>
	internal static string Truncate(string text, int maxLength)
	{
		if (text.Length <= maxLength)
		{
			return text;
		}

		var length = maxLength - 1;
		if (length > 0 && char.IsHighSurrogate(text[length - 1]))
		{
			length--;
		}

		return string.Concat(text.AsSpan(0, length), "…");
	}

	/// <summary>
	/// Как часто можно повторять оповещение об одной и той же проблеме
	/// </summary>
	/// <param name="problem"></param>
	/// <returns></returns>
	private static TimeSpan GetCooldown(ScheduleProblem problem) => problem switch
	{
		ScheduleProblem.PostFailed or ScheduleProblem.PostsInterrupted => TimeSpan.FromMinutes(30),
		ScheduleProblem.BotLostAccess => TimeSpan.FromHours(6),
		ScheduleProblem.QueueRunningLow or ScheduleProblem.QueueEmpty => TimeSpan.FromHours(24),
		// Суточный парсинг доходит до источника каждый раз в чуть разное время: ровно 24 ч пропускали бы день
		ScheduleProblem.ParsingFailed => TimeSpan.FromHours(20),
		_ => throw new ArgumentOutOfRangeException(nameof(problem), problem, null)
	};

	/// <summary>
	/// Описывает проблему человеческим языком
	/// </summary>
	/// <param name="problem"></param>
	/// <returns></returns>
	private static string Describe(ScheduleProblem problem) => problem switch
	{
		ScheduleProblem.PostFailed =>
			"Пост не опубликован и помечен ошибкой — его можно повторить",
		ScheduleProblem.BotLostAccess =>
			"Бот потерял доступ к каналу — проверьте, что он остался администратором с правом публикации",
		ScheduleProblem.PostsInterrupted =>
			"Отправка постов прервана перезапуском сервиса, посты помечены ошибкой — проверьте канал перед повтором",
		ScheduleProblem.QueueRunningLow =>
			"Запланированные посты скоро закончатся — добавьте новые",
		ScheduleProblem.QueueEmpty =>
			"Запланированных постов не осталось — публиковать нечего",
		ScheduleProblem.ParsingFailed =>
			"Не удалось спарсить источник постов",
		_ => "Неизвестная проблема с расписанием"
	};
}
