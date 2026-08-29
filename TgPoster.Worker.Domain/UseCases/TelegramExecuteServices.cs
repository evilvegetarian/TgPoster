using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgPoster.Worker.Domain.UseCases;

public sealed record TelegramSendResult(bool IsSuccess, int? MessageId = null)
{
	public static TelegramSendResult Success(int? messageId = null) => new(true, messageId);
	public static TelegramSendResult Failure() => new(false);
}

public class TelegramExecuteServices(ILogger<TelegramExecuteServices> logger)
{
	/// <summary>
	///     Отправляет медиа-группу с повтором при ошибках Telegram API
	/// </summary>
	/// <param name="telegramBot">Клиент бота</param>
	/// <param name="chatId">Идентификатор чата</param>
	/// <param name="albumFactory">
	///     Фабрика альбома. Вызывается заново перед каждой попыткой, поэтому обязана возвращать свежие
	///     потоки: HttpClient закрывает переданные потоки после завершения запроса
	/// </param>
	/// <param name="maxRetries">Максимальное количество повторов</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Отправленные сообщения</returns>
	public Task<Message[]> SendMedia(
		TelegramBotClient telegramBot,
		long chatId,
		Func<List<IAlbumInputMedia>> albumFactory,
		int maxRetries,
		CancellationToken ct
	)
	{
		return ExecuteWithRetryAsync(
			() => telegramBot.SendMediaGroup(chatId, albumFactory(),
				disableNotification: true, cancellationToken: ct),
			maxRetries, ct);
	}

	/// <summary>
	///     Отправляет фото с повтором при ошибках Telegram API
	/// </summary>
	/// <param name="telegramBot">Клиент бота</param>
	/// <param name="chatId">Идентификатор чата</param>
	/// <param name="photoFactory">
	///     Фабрика фото. Вызывается заново перед каждой попыткой, поэтому обязана возвращать свежий
	///     поток: HttpClient закрывает переданный поток после завершения запроса
	/// </param>
	/// <param name="maxRetries">Максимальное количество повторов</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Отправленное сообщение</returns>
	public Task<Message> SendPhoto(
		TelegramBotClient telegramBot,
		long chatId,
		Func<InputFileStream> photoFactory,
		int maxRetries,
		CancellationToken ct
	)
	{
		return ExecuteWithRetryAsync(
			() => telegramBot.SendPhoto(chatId, photoFactory(), cancellationToken: ct),
			maxRetries, ct);
	}

	public async Task<TelegramSendResult> SendMediaGroupAsync(
		ITelegramBotClient bot,
		long chatId,
		IEnumerable<IAlbumInputMedia> medias,
		CancellationToken ct
	)
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
		CancellationToken ct,
		ParseMode parseMode = ParseMode.None
	)
	{
		try
		{
			var msg = await ExecuteWithRetryAsync(
				() => bot.SendMessage(chatId, text, parseMode, cancellationToken: ct), ct: ct);
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
			catch (Exception ex) when (HasDisposedStream(ex))
			{
				// Контент запроса одноразовый: HttpClient закрывает потоки после завершения запроса.
				// Повтор с тем же контентом бессмысленен — пробрасываем, чтобы дефект был виден
				logger.LogError(ex,
					"Контент запроса к Telegram API уже освобождён. Фабрика контента обязана создавать новые потоки на каждую попытку");
				throw;
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
			catch (RequestException ex)
				when (ex.InnerException is TaskCanceledException && !ct.IsCancellationRequested)
			{
				retryCount++;
				if (retryCount > maxRetries)
				{
					logger.LogError(ex, "Достигнуто максимальное количество попыток после таймаута. Отказ.");
					throw;
				}

				var waitTime = TimeSpan.FromSeconds(Math.Pow(2, retryCount));

				logger.LogWarning(
					"Таймаут запроса к Telegram API. Повтор через {WaitTime} сек. Попытка {RetryCount}/{MaxRetries}",
					waitTime.TotalSeconds, retryCount, maxRetries);

				await Task.Delay(waitTime, ct);
			}
	}

	/// <summary>
	///     Проверяет, вызвана ли ошибка обращением к уже закрытому потоку
	/// </summary>
	/// <param name="exception">Проверяемое исключение</param>
	/// <returns><c>true</c>, если в цепочке исключений есть <see cref="ObjectDisposedException" /></returns>
	private static bool HasDisposedStream(Exception exception)
	{
		for (var current = exception; current is not null; current = current.InnerException)
			if (current is ObjectDisposedException)
				return true;

		return false;
	}
}