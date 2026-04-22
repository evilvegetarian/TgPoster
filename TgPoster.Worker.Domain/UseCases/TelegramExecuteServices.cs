using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace TgPoster.Worker.Domain.UseCases;

public sealed record TelegramSendResult(bool IsSuccess, int? MessageId = null)
{
	public static TelegramSendResult Success(int? messageId = null) => new(true, messageId);
	public static TelegramSendResult Failure() => new(false);
}

public class TelegramExecuteServices(ILogger<TelegramExecuteServices> logger)
{
	public Task<Telegram.Bot.Types.Message[]> SendMedia(
		TelegramBotClient telegramBot,
		long chatId,
		List<IAlbumInputMedia> album,
		int maxRetries,
		CancellationToken ct)
	{
		return ExecuteWithRetryAsync(
			() => telegramBot.SendMediaGroup(chatId, album,
				disableNotification: true, cancellationToken: ct),
			maxRetries, ct);
	}

	public Task<Telegram.Bot.Types.Message> SendPhoto(
		TelegramBotClient telegramBot,
		long chatId,
		InputFileStream photoStream,
		int maxRetries,
		CancellationToken ct)
	{
		return ExecuteWithRetryAsync(
			() => telegramBot.SendPhoto(chatId, photoStream, cancellationToken: ct),
			maxRetries, ct);
	}

	public async Task<TelegramSendResult> SendMediaGroupAsync(
		ITelegramBotClient bot,
		long chatId,
		IEnumerable<IAlbumInputMedia> medias,
		CancellationToken ct)
	{
		try
		{
			var msgs = await ExecuteWithRetryAsync(
				() => bot.SendMediaGroup(chatId, medias, cancellationToken: ct), ct: ct);
			return TelegramSendResult.Success(msgs.FirstOrDefault()?.MessageId);
		}
		catch (RequestException ex) when (ex.InnerException is TaskCanceledException)
		{
			logger.LogWarning(ex, "Таймаут при отправке медиа-группы в чат {ChatId}", chatId);
			return TelegramSendResult.Failure();
		}
		catch (RequestException ex)
		{
			logger.LogError(ex, "Ошибка при отправке медиа-группы в чат {ChatId}", chatId);
			return TelegramSendResult.Failure();
		}
	}

	public async Task<TelegramSendResult> SendTextAsync(
		ITelegramBotClient bot,
		long chatId,
		string text,
		CancellationToken ct)
	{
		try
		{
			var msg = await ExecuteWithRetryAsync(
				() => bot.SendMessage(chatId, text, cancellationToken: ct), ct: ct);
			return TelegramSendResult.Success(msg.MessageId);
		}
		catch (RequestException ex) when (ex.InnerException is TaskCanceledException)
		{
			logger.LogWarning(ex, "Таймаут при отправке сообщения в чат {ChatId}", chatId);
			return TelegramSendResult.Failure();
		}
		catch (RequestException ex)
		{
			logger.LogError(ex, "Ошибка при отправке сообщения в чат {ChatId}", chatId);
			return TelegramSendResult.Failure();
		}
	}

	/// <summary>
	///     Выполняет асинхронную операцию с обработкой ошибок ограничения скорости Telegram Bot API (429).
	/// </summary>
	private async Task<T> ExecuteWithRetryAsync<T>(
		Func<Task<T>> apiCall,
		int maxRetries = 3,
		CancellationToken ct = default
	)
	{
		var retryCount = 0;
		while (true)
			try
			{
				return await apiCall();
			}
			catch (ApiRequestException ex) when (ex.ErrorCode == 429)
			{
				retryCount++;
				if (retryCount > maxRetries)
				{
					logger.LogError(ex, "Достигнуто максимальное количество попыток для API вызова. Отказ.");
					throw;
				}

				var retryAfter = ex.Parameters?.RetryAfter ?? 30;
				var waitTime = TimeSpan.FromSeconds(retryAfter + 1);

				logger.LogWarning(
					"Получен лимит запросов от Telegram API. Ожидание: {WaitTime} сек. Попытка {RetryCount}/{MaxRetries}",
					retryAfter, retryCount, maxRetries);

				await Task.Delay(waitTime, ct);
			}
	}
}
