using Microsoft.Extensions.Logging;
using Shared.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace TgPoster.API.Domain.Services;

internal sealed class PublishedMessageService(
	TelegramBotManager botManager,
	ILogger<PublishedMessageService> logger
) : IPublishedMessageService
{
	private const string NotModifiedError = "message is not modified";

	public async Task<ApiResult> EditTextAsync(
		string token,
		long channelId,
		int messageId,
		bool isCaption,
		string html,
		CancellationToken ct
	)
	{
		var bot = botManager.GetClient(token);
		try
		{
			if (isCaption)
			{
				await bot.EditMessageCaption(channelId, messageId, html, ParseMode.Html, cancellationToken: ct);
			}
			else
			{
				await bot.EditMessageText(channelId, messageId, html, ParseMode.Html, cancellationToken: ct);
			}

			return ApiResult.Success();
		}
		catch (ApiRequestException ex) when (ex.Message.Contains(NotModifiedError, StringComparison.OrdinalIgnoreCase))
		{
			// Текст в канале уже такой же — для пользователя это успешная правка
			return ApiResult.Success();
		}
		catch (RequestException ex)
		{
			logger.LogWarning(ex, "Не удалось изменить текст сообщения {MessageId} в канале {ChannelId}",
				messageId, channelId);
			return ApiResult.Failure(ex.Message);
		}
	}

	public async Task<ApiResult> DeleteAsync(string token, long channelId, int[] ids, CancellationToken ct)
	{
		var bot = botManager.GetClient(token);
		try
		{
			await bot.DeleteMessages(channelId, ids, ct);
			return ApiResult.Success();
		}
		catch (RequestException ex)
		{
			logger.LogWarning(ex, "Не удалось удалить сообщения поста из канала {ChannelId}", channelId);
			return ApiResult.Failure(ex.Message);
		}
	}
}
