using System.Text.Json;
using System.Text.RegularExpressions;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Classification;
using Shared.Enums;
using Shared.OpenRouter;
using Shared.OpenRouter.Models.Request;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

internal sealed partial class ClassifyChannelWorker(
	IOpenRouterClient openRouterClient,
	IClassifyChannelStorage storage,
	ITelegramAuthService authService,
	ITelegramMessageService tgMessages,
	IWorkerJobStatusStorage statusStorage,
	TimeProvider timeProvider,
	OpenRouterOptions options,
	ILogger<ClassifyChannelWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int MaxImageSize = 512;
	private const int JpegQuality = 80;
	private const int MaxTextLength = 500;

	private const string ClassificationUserPromptTemplate = """
	                                                        Название канала: {0}
	                                                        Описание: {1}
	                                                        Последние посты (--- разделитель):
	                                                        {2}
	                                                        """;

	/// <summary>
	///     Через сколько повторять канал, классификация которого не удалась
	/// </summary>
	private static readonly TimeSpan RetryDelay = TimeSpan.FromHours(6);

	/// <summary>
	///     Job тикает раз в минуту, поэтому интервал из настроек сверяем с запасом —
	///     иначе каждый запуск съезжал бы на лишнюю минуту
	/// </summary>
	private static readonly TimeSpan ScheduleTolerance = TimeSpan.FromSeconds(30);

	private static readonly SemaphoreSlim ParseLock = new(1, 1);

	/// <summary>
	///     Настройки по умолчанию: модель и размер выборки берутся из конфига воркера, чтобы после обновления
	///     классификатор продолжил работать как раньше, остальное — из <see cref="ClassifierDefaults" />
	/// </summary>
	/// <param name="options"></param>
	/// <returns></returns>
	internal static ClassifierSettingsDto CreateDefaultSettings(OpenRouterOptions options) => new()
	{
		IsEnabled = ClassifierDefaults.IsEnabled,
		Model = string.IsNullOrWhiteSpace(options.Model) ? ClassifierDefaults.Model : options.Model,
		BatchSize = ClassifierDefaults.BatchSize,
		IntervalMinutes = ClassifierDefaults.IntervalMinutes,
		MessageSampleCount = options.MessageSampleCount > 0
			? options.MessageSampleCount
			: ClassifierDefaults.MessageSampleCount,
		PhotoCount = ClassifierDefaults.PhotoCount,
		ReclassifyAfterDays = null,
		Categories = ClassifierDefaults.Categories,
		SystemPrompt = ClassifierDefaults.SystemPrompt
	};

	[DisableConcurrentExecution(100000)]
	public async Task ClassifyChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		if (!await ParseLock.WaitAsync(0, ct))
		{
			return;
		}

		try
		{
			var settings = await storage.GetSettingsAsync(ct) ?? CreateDefaultSettings(options);
			if (!settings.IsEnabled)
			{
				return;
			}

			// Hangfire дёргает job каждую минуту, а реальный интервал берётся из настроек
			var startedAt = timeProvider.GetUtcNow();
			var interval = TimeSpan.FromMinutes(settings.IntervalMinutes);
			var lastStartedAt = await statusStorage.GetLastStartedAtAsync(WorkerJobNames.ClassifyChannels, ct);
			if (lastStartedAt is not null && startedAt - lastStartedAt < interval - ScheduleTolerance)
			{
				return;
			}

			await TryReportAsync(() => statusStorage.ReportStartedAsync(WorkerJobNames.ClassifyChannels, ct));

			var error = await ClassifyBatchAsync(settings, ct);

			// Финальную запись статуса делаем с CancellationToken.None: при остановке приложения
			// она должна успеть выполниться best-effort
			var nextRunAt = startedAt + interval;
			if (error is null)
			{
				await TryReportAsync(() => statusStorage.ReportCompletedAsync(
					WorkerJobNames.ClassifyChannels, nextRunAt, CancellationToken.None));
			}
			else
			{
				await TryReportAsync(() => statusStorage.ReportFailedAsync(
					WorkerJobNames.ClassifyChannels, error, nextRunAt, CancellationToken.None));
			}
		}
		catch (OperationCanceledException)
		{
			// Статус не пишем: запись останется Running, и API покажет «прервана» по протухшему heartbeat'у
			throw;
		}
		catch (Exception ex)
		{
			await TryReportAsync(() => statusStorage.ReportFailedAsync(
				WorkerJobNames.ClassifyChannels,
				ex.Message,
				null,
				CancellationToken.None));
			throw;
		}
		finally
		{
			ParseLock.Release();
		}
	}

	/// <summary>
	///     Классифицировать очередную пачку каналов
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="ct"></param>
	/// <returns>Текст ошибки, если запуск не смог выполнить работу, иначе null</returns>
	private async Task<string?> ClassifyBatchAsync(ClassifierSettingsDto settings, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(options.SecretKey))
		{
			logger.LogWarning("Ключ OpenRouter не задан, пропускаем классификацию каналов");
			return "Не задан ключ OpenRouter (OpenRouterOptions:SecretKey в конфиге воркера)";
		}

		var now = timeProvider.GetUtcNow();
		var channels = await storage.GetChannelsToClassifyAsync(
			settings.BatchSize,
			now - RetryDelay,
			settings.ReclassifyAfterDays is { } days ? now.AddDays(-days) : null,
			ct);
		if (channels.Count == 0)
		{
			return null;
		}

		var sessionId = settings.TelegramSessionId
		                ?? await authService.GetSessionIdForPurposeAsync(TelegramSessionPurpose.Classification, ct);
		if (sessionId is null)
		{
			logger.LogWarning("Нет Telegram-сессии для классификации каналов");
			return "Не выбрана Telegram-сессия: укажите её в настройках классификатора";
		}

		string? lastError = null;
		var failedCount = 0;
		for (var index = 0; index < channels.Count; index++)
		{
			var channel = channels[index];
			var processed = index;
			await TryReportAsync(() => statusStorage.ReportHeartbeatAsync(
				WorkerJobNames.ClassifyChannels,
				processed,
				channels.Count,
				channel.Username is null ? channel.Title : $"@{channel.Username}",
				ct));

			try
			{
				await storage.MarkClassificationAttemptAsync(channel.Id, timeProvider.GetUtcNow(), ct);
				await ClassifyChannelAsync(channel, sessionId.Value, settings, ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				logger.LogError(ex, "Ошибка при классификации канала {ChannelId}", channel.Id);
				failedCount++;
				lastError = ex.Message;
			}
		}

		// Единичный сбой — норма (канал мог пропасть), а вот если упали все каналы пачки,
		// скорее всего сломан сам классификатор: ключ, модель или сессия
		return failedCount == channels.Count ? lastError : null;
	}

	/// <summary>
	///     Выполнить запись статуса, проглатывая ошибки: сбой записи не должен ронять job
	/// </summary>
	/// <param name="report"></param>
	private async Task TryReportAsync(Func<Task> report)
	{
		try
		{
			await report();
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Не удалось записать статус задачи {JobName}", WorkerJobNames.ClassifyChannels);
		}
	}

	private async Task ClassifyChannelAsync(
		ChannelForClassificationDto channel,
		Guid sessionId,
		ClassifierSettingsDto settings,
		CancellationToken ct
	)
	{
		var resolved = await ResolvePeerAsync(sessionId, channel, ct);
		if (resolved is null)
		{
			logger.LogDebug("Не удалось найти канал {ChannelId} в Telegram для загрузки сообщений", channel.Id);
			return;
		}

		var sample = await FetchRecentMessagesAsync(
			sessionId, resolved, settings.MessageSampleCount, settings.PhotoCount, ct);

		var messagesSection = sample.Texts.Count > 0
			? string.Join("\n---\n", sample.Texts)
			: "(нет данных)";

		var userPrompt = string.Format(
			ClassificationUserPromptTemplate,
			channel.Title ?? "",
			channel.Description ?? "",
			messagesSection);

		var contentParts = new List<MessageContentPart>
		{
			new() { Type = "text", Text = userPrompt }
		};

		if (sample.Photos.Count > 0)
		{
			var images = await DownloadAndPrepareImagesAsync(sessionId, resolved, sample.Photos, ct);
			contentParts.AddRange(images.Select(url => new MessageContentPart
			{
				Type = "image_url",
				ImageUrl = new ImageUrlInfo { Url = url }
			}));
		}

		var messages = new List<ChatMessage>
		{
			new() { Role = "system", Content = BuildSystemPrompt(settings) },
			new() { Role = "user", Content = contentParts }
		};

		var response = await openRouterClient.SendMessageRawAsync(
			options.SecretKey!,
			settings.Model,
			messages,
			ct);

		var content = response.Choices.FirstOrDefault()?.Message.Content.ToString();
		if (string.IsNullOrWhiteSpace(content))
		{
			logger.LogWarning("Пустой ответ от LLM для канала {ChannelId}", channel.Id);
			return;
		}

		var classification = ParseClassification(content);
		if (classification is null)
		{
			logger.LogWarning("Не удалось распарсить ответ LLM для канала {ChannelId}: {Content}", channel.Id, content);
			return;
		}

		await storage.UpdateClassificationAsync(
			channel.Id,
			classification.Category,
			classification.Subcategory,
			classification.Tags,
			classification.Language,
			classification.Confidence,
			ct);

		logger.LogDebug(
			"Канал {Channel} классифицирован: {Category}/{Subcategory} (confidence: {Confidence})",
			channel.Title, classification.Category, classification.Subcategory, classification.Confidence);
	}

	/// <summary>
	///     Подставить список тематик в системный промпт из настроек
	/// </summary>
	/// <param name="settings"></param>
	/// <returns></returns>
	private static string BuildSystemPrompt(ClassifierSettingsDto settings) =>
		settings.SystemPrompt.Replace(ClassifierDefaults.CategoriesPlaceholder, string.Join(", ", settings.Categories));

	private async Task<RecentMessagesSample> FetchRecentMessagesAsync(
		Guid sessionId,
		TelegramPeer peer,
		int messageCount,
		int maxPhotoCount,
		CancellationToken ct
	)
	{
		var historyResult = await tgMessages.GetHistoryAsync(
			sessionId, peer, messageCount, ct: ct);

		if (!historyResult.IsSuccess)
		{
			logger.LogDebug("Не удалось получить сообщения канала {ChannelId}: {Status} {Error}",
				peer.Id, historyResult.Status, historyResult.ErrorMessage);
			return new RecentMessagesSample([], []);
		}

		var texts = new List<string>();
		var photos = new List<PhotoForClassification>();

		foreach (var message in historyResult.Value!.Messages)
		{
			if (!string.IsNullOrWhiteSpace(message.Text))
			{
				var cleaned = CleanMessageText(message.Text);
				if (!string.IsNullOrWhiteSpace(cleaned))
				{
					texts.Add(TruncateMessage(cleaned, MaxTextLength));
				}
			}

			if (photos.Count < maxPhotoCount
			    && message.Media is { Type: TelegramMediaType.Photo } media)
			{
				photos.Add(new PhotoForClassification(message.Id, media));
			}
		}

		logger.LogDebug("Загружено {TextCount} сообщений и {PhotoCount} фото для канала {ChannelId}",
			texts.Count, photos.Count, peer.Id);
		return new RecentMessagesSample(texts, photos);
	}

	private async Task<List<string>> DownloadAndPrepareImagesAsync(
		Guid sessionId,
		TelegramPeer channel,
		List<PhotoForClassification> photos,
		CancellationToken ct
	)
	{
		var results = new List<string>();

		foreach (var item in photos)
		{
			try
			{
				await using var rawStream = new MemoryStream();
				var downloadResult = await tgMessages.DownloadMediaAsync(
					sessionId, channel, item.MessageId, item.Media, rawStream, ct);

				if (!downloadResult.IsSuccess)
				{
					logger.LogDebug("Не удалось скачать фото для сообщения {MessageId}: {Status} {Error}",
						item.MessageId, downloadResult.Status, downloadResult.ErrorMessage);
					continue;
				}

				rawStream.Position = 0;
				using var image = await Image.LoadAsync(rawStream, ct);
				image.Mutate(x => x.Resize(new ResizeOptions
				{
					Size = new Size(MaxImageSize, MaxImageSize),
					Mode = ResizeMode.Max
				}));

				using var outStream = new MemoryStream();
				await image.SaveAsJpegAsync(outStream, new JpegEncoder { Quality = JpegQuality }, ct);

				results.Add(openRouterClient.ToLocalImageDataUrl(outStream.ToArray()));
			}
			catch (Exception ex)
			{
				logger.LogDebug(ex, "Ошибка обработки фото из сообщения {MessageId}", item.MessageId);
			}
		}

		return results;
	}

	private async Task<TelegramPeer?> ResolvePeerAsync(
		Guid sessionId,
		ChannelForClassificationDto channel,
		CancellationToken ct
	)
	{
		if (!string.IsNullOrEmpty(channel.Username))
		{
			var resolved = await tgMessages.ResolveChannelAsync(sessionId, channel.Username, ct);
			if (await resolved.HandleChannelUnavailableAsync(async () =>
				    await storage.MarkChannelBannedAsync(channel.Id, ct)))
			{
				return null;
			}

			return resolved.IsSuccess ? resolved.Value!.Peer : null;
		}

		if (channel.TelegramId.HasValue)
		{
			var dialogsResult = await tgMessages.GetAllDialogsAsync(sessionId, ct);
			if (!dialogsResult.IsSuccess)
			{
				return null;
			}

			var found = dialogsResult.Value!.FirstOrDefault(c => c.Id == channel.TelegramId.Value);
			return found?.Peer;
		}

		return null;
	}

	private static string TruncateMessage(string text, int maxLength)
	{
		if (text.Length <= maxLength)
		{
			return text;
		}

		return string.Concat(text.AsSpan(0, maxLength), "...");
	}

	private static string CleanMessageText(string text)
	{
		var stripped = UrlRegex().Replace(text, "");
		stripped = TelegramLinkRegex().Replace(stripped, "");
		stripped = SubscriptionTailRegex().Replace(stripped, "");
		stripped = MultiNewlineRegex().Replace(stripped, "\n\n");
		return stripped.Trim();
	}

	private static ChannelClassificationResult? ParseClassification(string content)
	{
		var json = content.Trim();

		if (json.StartsWith("```"))
		{
			var startIndex = json.IndexOf('{');
			var endIndex = json.LastIndexOf('}');
			if (startIndex >= 0 && endIndex > startIndex)
			{
				json = json[startIndex..(endIndex + 1)];
			}
		}

		try
		{
			return JsonSerializer.Deserialize<ChannelClassificationResult>(json);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	[GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
	private static partial Regex UrlRegex();

	[GeneratedRegex(@"\bt\.me/\S+", RegexOptions.IgnoreCase)]
	private static partial Regex TelegramLinkRegex();

	[GeneratedRegex(@"(?:^|\n)[^\n]*(подпишись|подписывайся|наш канал|наш паблик|subscribe|join us|👉|🔔|📢)[^\n]*$",
		RegexOptions.IgnoreCase)]
	private static partial Regex SubscriptionTailRegex();

	[GeneratedRegex(@"\n{3,}")]
	private static partial Regex MultiNewlineRegex();

	private sealed record RecentMessagesSample(List<string> Texts, List<PhotoForClassification> Photos);

	private sealed record PhotoForClassification(int MessageId, TelegramMessageMedia Media);
}