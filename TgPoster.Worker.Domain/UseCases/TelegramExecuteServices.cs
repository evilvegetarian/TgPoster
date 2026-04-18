using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace TgPoster.Worker.Domain.UseCases;

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
