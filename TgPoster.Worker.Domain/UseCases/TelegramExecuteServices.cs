using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using TL;
using WTelegram;
using Document = TL.Document;
using Message = TL.Message;

namespace TgPoster.Worker.Domain.UseCases;

public class TelegramExecuteServices(ILogger<TelegramExecuteServices> logger)
{
	public async Task<Telegram.Bot.Types.Message[]> SendMedia(
		TelegramBotClient telegramBot,
		long chatId,
		List<IAlbumInputMedia> album,
		int maxRetries,
		CancellationToken ct
	)
	{
		var retryCount = 0;
		while (true)
			try
			{
				var messages = await telegramBot.SendMediaGroup(
					chatId,
					album,
					disableNotification: true,
					cancellationToken: ct);
				return messages;
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

	public async Task<Telegram.Bot.Types.Message> SendPhoto(
		TelegramBotClient telegramBot,
		long chatId,
		InputFileStream photoStream,
		int maxRetries,
		CancellationToken ct
	)
	{
		var retryCount = 0;
		while (true)
			try
			{
				var photoMessage = await telegramBot.SendPhoto(chatId, photoStream, cancellationToken: ct);
				return photoMessage;
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

	public async Task DownloadVideoAsync(
		Client client,
		InputPeerChannel channel,
		Document video,
		Stream stream,
		int messageId
	)
	{
		try
		{
			await client.DownloadFileAsync(video, stream);
		}
		catch (RpcException ex) when (ex.Message == "FILE_REFERENCE_EXPIRED")
		{
			var refreshedMessages =
				await client.Channels_GetMessages(new InputChannel(channel.channel_id, channel.access_hash), messageId);

			if (refreshedMessages.Messages.FirstOrDefault() is Message
			    {
				    media: MessageMediaDocument { document: Document refreshedDoc }
			    })
			{
				await client.DownloadFileAsync(refreshedDoc, stream);
			}
			else
			{
				throw;
			}
		}
	}

	public async Task DownloadPhotoAsync(
		Client client,
		Channel channel,
		Photo photo,
		Stream stream,
		int messageId
	)
	{
		try
		{
			await client.DownloadFileAsync(photo, stream);
		}
		catch (RpcException ex) when (ex.Message == "FILE_REFERENCE_EXPIRED")
		{
			var refreshedMessages =
				await client.Channels_GetMessages(new InputChannel(channel.ID, channel.access_hash), messageId);

			if (refreshedMessages.Messages.FirstOrDefault() is Message
			    {
				    media: MessageMediaPhoto { photo: Photo refreshedPhoto }
			    })
			{
				await client.DownloadFileAsync(refreshedPhoto, stream);
			}
			else
			{
				throw;
			}
		}
	}


	/// <summary>
	///     Выполняет асинхронную операцию с обработкой ошибок ограничения скорости Telegram.
	/// </summary>
	/// <param name="apiCall">Функция, представляющая вызов API Telegram, не возвращающая результат.</param>
	/// <param name="maxRetries">Максимальное количество повторных попыток.</param>
	/// <param name="ct">Токен отмены.</param>
	private async Task ExecuteWithRetryAsync(Func<Task> apiCall, int maxRetries = 3, CancellationToken ct = default)
	{
		var retryCount = 0;
		while (true)
			try
			{
				await apiCall();
				return;
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

	/// <summary>
	///     Выполняет асинхронную операцию с обработкой ошибок ограничения скорости Telegram.
	/// </summary>
	/// <typeparam name="T">Тип возвращаемого значения.</typeparam>
	/// <param name="apiCall">Функция, представляющая вызов API Telegram, возвращающая результат типа T.</param>
	/// <param name="maxRetries">Максимальное количество повторных попыток.</param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>Результат выполнения API вызова.</returns>
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