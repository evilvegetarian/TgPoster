using System.Text;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using Telegram.Bot;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Internal;

/// <summary>
///     Отправляет владельцу оповещения о проблемах с Telegram аккаунтом через привязанного к сессии бота
/// </summary>
internal sealed class TelegramSessionAlertService(
	ILogger<TelegramSessionAlertService> logger,
	ITelegramSessionAlertRepository repository,
	SessionAlertThrottle throttle,
	TelegramBotManager botManager) : ITelegramSessionAlertService
{
	public async Task NotifyAsync(
		Guid sessionId,
		TelegramSessionProblem problem,
		string? details = null,
		CancellationToken ct = default
	)
	{
		try
		{
			if (!throttle.TryAcquire(sessionId, problem))
			{
				logger.LogDebug(
					"Оповещение о проблеме {Problem} с сессией {SessionId} пропущено: недавно уже отправляли",
					problem, sessionId);
				return;
			}

			var target = await repository.GetAlertTargetAsync(sessionId, ct);
			if (target is null)
			{
				logger.LogDebug(
					"Оповещение о проблеме {Problem} с сессией {SessionId} не отправлено: бот не привязан",
					problem, sessionId);
				return;
			}

			var bot = botManager.GetClient(target.BotToken);
			await bot.SendMessage(target.ChatId, BuildMessage(target, problem, details), cancellationToken: ct);

			logger.LogInformation("Отправлено оповещение о проблеме {Problem} с сессией {SessionId}",
				problem, sessionId);
		}
		catch (Exception ex)
		{
			// Сбой оповещения не должен ломать основной сценарий
			logger.LogError(ex, "Не удалось отправить оповещение о проблеме {Problem} с сессией {SessionId}",
				problem, sessionId);
		}
	}

	/// <summary>
	///     Собирает текст оповещения для владельца аккаунта
	/// </summary>
	/// <param name="target">Данные аккаунта и бота-оповещателя</param>
	/// <param name="problem">Тип проблемы</param>
	/// <param name="details">Технические подробности от Telegram</param>
	/// <returns>Готовый текст сообщения</returns>
	private static string BuildMessage(
		TelegramSessionAlertTarget target,
		TelegramSessionProblem problem,
		string? details
	)
	{
		var account = string.IsNullOrWhiteSpace(target.SessionName)
			? target.PhoneNumber
			: $"{target.SessionName} ({target.PhoneNumber})";

		var message = new StringBuilder()
			.AppendLine("⚠️ Проблема с Telegram аккаунтом")
			.AppendLine($"Аккаунт: {account}")
			.AppendLine(Describe(problem));

		if (!string.IsNullOrWhiteSpace(details))
		{
			message.AppendLine($"Подробности: {details}");
		}

		return message.ToString();
	}

	/// <summary>
	///     Описывает проблему человеческим языком
	/// </summary>
	/// <param name="problem">Тип проблемы</param>
	/// <returns>Описание проблемы и что с ней делать</returns>
	private static string Describe(TelegramSessionProblem problem) => problem switch
	{
		TelegramSessionProblem.AuthorizationRevoked =>
			"Telegram отозвал авторизацию — войдите в аккаунт заново",
		TelegramSessionProblem.AuthKeyDuplicated =>
			"Ключ авторизации используется где-то ещё (AUTH_KEY_DUPLICATED), сессия отключена — авторизуйтесь заново",
		TelegramSessionProblem.SessionCorrupted =>
			"Данные сессии повреждены, сессия отключена — импортируйте или авторизуйте её заново",
		TelegramSessionProblem.LoginFailed =>
			"Не удалось войти в аккаунт, сессия отключена",
		TelegramSessionProblem.FloodWait =>
			"Telegram временно ограничил аккаунт по частоте запросов (FLOOD_WAIT)",
		TelegramSessionProblem.SpamRestricted =>
			"Аккаунт ограничен за спам (PEER_FLOOD) — часть действий недоступна",
		_ => "Неизвестная проблема с аккаунтом"
	};
}
