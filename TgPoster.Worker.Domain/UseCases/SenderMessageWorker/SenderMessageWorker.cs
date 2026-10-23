using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Hangfire;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Security.Cryptography;
using Shared.CrossPosting;
using Shared.Telegram;
using Shared.Utilities;
using Shared.YouTube;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public class SenderMessageWorker(
	ISenderMessageStorage storage,
	ILogger<SenderMessageWorker> logger,
	ICryptoAES crypto,
	TelegramOptions options,
	YouTubeService youTubeService,
	IPublishEndpoint publishEndpoint,
	TelegramBotManager botManager,
	IHostApplicationLifetime lifetime,
	TelegramExecuteServices telegramExecuteServices,
	IScheduleAlertService scheduleAlertService
)
{
	private const string UnknownTelegramError = "Неизвестная ошибка Telegram";
	private const int PostPreviewLength = 80;

	private const string InterruptedByRestartError =
		"Отправка прервана перезапуском воркера. Проверьте канал перед повтором";

	private static readonly TimeSpan RecoveryAlertsTimeout = TimeSpan.FromSeconds(30);

	public async Task<List<InterruptedMessage>> RecoverAfterRestartAsync(CancellationToken ct)
	{
		var interrupted = await storage.RecoverInHandleMessagesAsync(DateTimeOffset.UtcNow,
			InterruptedByRestartError, ct);

		logger.LogInformation(
			"Восстановление после перезапуска воркера: будущие зависшие посты возвращены в очередь, прошедших помечено ошибкой: {InterruptedCount}",
			interrupted.Count);

		// Метод вызывается синхронно при старте воркера: недоступный Telegram не должен надолго задерживать запуск
		using var alertsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		alertsCts.CancelAfter(RecoveryAlertsTimeout);
		foreach (var schedule in interrupted.GroupBy(x => x.ScheduleId))
		{
			await scheduleAlertService.NotifyAsync(schedule.Key, ScheduleProblem.PostsInterrupted,
				$"Прервано постов: {schedule.Count()}", ct: alertsCts.Token);
		}

		return interrupted;
	}

	public async Task ProcessMessagesAsync()
	{
		var messageDetails = await storage.GetMessagesAsync();
		var messages = messageDetails.SelectMany(x => x.MessageDto).ToList();
		if (messages.Count == 0)
		{
			return;
		}

		await storage.UpdateStatusInHandleMessageAsync(messages.Select(x => x.Id).ToList());
		foreach (var detail in messageDetails)
		{
			var token = crypto.Decrypt(options.SecretKey, detail.Api);

			foreach (var message in detail.MessageDto.OrderBy(x => x.TimePosting))
			{
				try
				{
					BackgroundJob.Schedule<SenderMessageWorker>(
						x => x.SendMessageAsync(message.Id, token, detail.ChannelId, message, detail.YouTubeAccount),
						message.TimePosting);
				}
				catch (Exception e)
				{
					logger.LogError(e, "Ошибка во время отправки сообщения {id}", message.Id);
					await storage.UpdateErrorStatusMessageAsync(message.Id,
						$"Не удалось запланировать отправку: {e.Message}", lifetime.ApplicationStopping);
				}
			}
		}
	}

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public async Task SendMessageAsync(
		Guid messageId,
		string token,
		long chatId,
		MessageDto message,
		YouTubeAccountDto? youTubeAccount
	)
	{
		var ct = lifetime.ApplicationStopping;
		var bot = botManager.GetClient(token);
		var medias = message.File.Select(file => (InputMedia)(file.ContentType.GetFileType() == FileTypes.Image
				? new InputMediaPhoto(file.TgFileId)
				: new InputMediaVideo(file.TgFileId)))
			.ToList();

		int? telegramMessageId;

		if (medias.Any())
		{
			// Длинный текст поста и без подписи уходит отдельным сообщением, поэтому лимит выбирается
			// по самому тексту: подпись не должна превращать такой пост в обрезанный caption
			var isCaptionTooLong = (message.Message?.Length ?? 0) > TelegramLimits.CaptionLength;
			var limit = isCaptionTooLong ? TelegramLimits.MessageLength : TelegramLimits.CaptionLength;
			var captionText = PostSignatureComposer.Compose(message.Message, message.Signature, limit);

			if (!string.IsNullOrWhiteSpace(captionText) && !isCaptionTooLong)
			{
				medias[0].Caption = captionText;
				medias[0].ParseMode = ParseMode.Html;
			}

			var result =
				await telegramExecuteServices.SendMediaGroupAsync(bot, chatId, medias.Select(x => (IAlbumInputMedia)x),
					ct);
			if (!result.IsSuccess)
			{
				await HandleSendFailureAsync(messageId, message, result, ct);
				return;
			}

			telegramMessageId = result.MessageId;

			if (!string.IsNullOrWhiteSpace(captionText) && isCaptionTooLong)
			{
				var captionResult =
					await telegramExecuteServices.SendTextAsync(bot, chatId, captionText, ct, ParseMode.Html);
				if (!captionResult.IsSuccess)
					logger.LogWarning("Не удалось отправить подпись к медиа-группе для сообщения {MessageId}",
						messageId);
			}
		}
		else
		{
			var text = PostSignatureComposer.Compose(message.Message, message.Signature,
				TelegramLimits.MessageLength);
			var result = await telegramExecuteServices.SendTextAsync(bot, chatId, text, ct, ParseMode.Html);
			if (!result.IsSuccess)
			{
				await HandleSendFailureAsync(messageId, message, result, ct);
				return;
			}

			telegramMessageId = result.MessageId;
		}

		await storage.UpdateSendStatusMessageAsync(messageId);

		if (telegramMessageId.HasValue)
		{
			await storage.SaveTelegramMessageIdAsync(messageId, telegramMessageId.Value, ct);

			var repostSettingsList = await storage.GetRepostSettingsForMessageAsync(messageId, ct);
			foreach (var repostSettings in repostSettingsList.Where(rs => rs.Destinations.Count > 0))
			{
				var command = new RepostMessageCommand
				{
					MessageId = messageId,
					ScheduleId = repostSettings.ScheduleId,
					RepostSettingsId = repostSettings.Id
				};

				await publishEndpoint.Publish(command, ct);
			}
		}

		if (youTubeAccount?.AutoPostingVideo == true)
		{
			await UploadVideosToYouTubeAsync(bot, message, youTubeAccount);
		}
	}

	/// <summary>
	/// Помечает пост ошибкой и оповещает владельца расписания
	/// </summary>
	/// <param name="messageId"></param>
	/// <param name="message"></param>
	/// <param name="result"></param>
	/// <param name="ct"></param>
	private async Task HandleSendFailureAsync(
		Guid messageId,
		MessageDto message,
		TelegramSendResult result,
		CancellationToken ct
	)
	{
		var error = result.Error ?? UnknownTelegramError;
		await storage.UpdateErrorStatusMessageAsync(messageId, error, ct);

		var problem = result.IsAccessLost ? ScheduleProblem.BotLostAccess : ScheduleProblem.PostFailed;
		await scheduleAlertService.NotifyAsync(message.ScheduleId, problem, BuildPostFailedDetails(message, error),
			ct: ct);
	}

	/// <summary>
	/// Собирает подробности оповещения о неотправленном посте: время, причина и начало текста поста
	/// </summary>
	/// <param name="message"></param>
	/// <param name="error"></param>
	/// <returns></returns>
	internal static string BuildPostFailedDetails(MessageDto message, string error)
	{
		var time = message.TimePosting.UtcDateTime.ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
		var details = $"Пост на {time} UTC не отправлен: {error}";

		// Пост хранится в Telegram-HTML, а оповещение уходит простым текстом — теги в превью только мешают
		var preview = TelegramHtmlToText.Convert(message.Message);
		return string.IsNullOrEmpty(preview)
			? details
			: $"{details}\nТекст: {ScheduleAlertService.Truncate(preview, PostPreviewLength)}";
	}

	private async Task UploadVideosToYouTubeAsync(
		ITelegramBotClient bot,
		MessageDto message,
		YouTubeAccountDto youTubeAccount
	)
	{
		var videoFiles = message.File
			.Where(f => f.ContentType.GetFileType() == FileTypes.Video)
			.ToList();

		if (videoFiles.Count == 0)
		{
			return;
		}

		foreach (var videoFile in videoFiles)
		{
			try
			{
				using var stream = new MemoryStream();
				await bot.GetInfoAndDownloadFile(videoFile.TgFileId, stream, lifetime.ApplicationStopping);

				var result = await youTubeService.UploadVideoAsync(youTubeAccount, stream);

				await storage.UpdateYouTubeTokensAsync(youTubeAccount.Id, result.AccessToken, result.RefreshToken,
					lifetime.ApplicationStopping);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при загрузке видео на YouTube");
			}
		}
	}
}