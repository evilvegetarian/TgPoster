using System.Text.Json;
using System.Text.RegularExpressions;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.OpenRouter;
using Shared.OpenRouter.Models.Request;
using Shared.Telegram;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using TL;
using TgPoster.Worker.Domain.ConfigModels;
using Message = TL.Message;

namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

internal sealed partial class ClassifyChannelWorker(
	IOpenRouterClient openRouterClient,
	IClassifyChannelStorage storage,
	ITelegramAuthService authService,
	ITelegramMessageService tgMessages,
	OpenRouterOptions options,
	ILogger<ClassifyChannelWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int BatchSize = 1;
	private const int MaxPhotoCount = 6;
	private const int MaxImageSize = 512;
	private const int JpegQuality = 80;
	private const int MaxTextLength = 500;
	private static readonly SemaphoreSlim ParseLock = new(1, 1);

	private const string ClassificationSystemPrompt = """
	                                                  Ты — классификатор Telegram-каналов. На вход получаешь название, описание,
	                                                  последние посты и до 6 фотографий из этих постов. Используй И текст, И визуальный
	                                                  контекст изображений для определения тематики.

	                                                  Категории (выбери РОВНО ОДНУ): Технологии, Новости, Крипто, Бизнес, Маркетинг,
	                                                  Развлечения, Образование, Политика, Спорт, Здоровье, Путешествия, Еда, Музыка,
	                                                  Игры, Авто, Финансы, Наука, Дизайн, Юмор, 18+, Другое.

	                                                  Правила:
	                                                  - Если данных мало или они противоречивы — ставь confidence < 0.5 и категорию "Другое".
	                                                  - tags: 3-5 коротких тегов (1-2 слова каждый), отражающих узкую специфику.
	                                                  - language: ISO-код основного языка постов (ru, en, uk, ...).
	                                                  - Отвечай СТРОГО валидным JSON без markdown-обёрток, без ```, без комментариев.

	                                                  Формат ответа:
	                                                  {"category":"...","subcategory":"...","tags":["...","..."],"language":"...","confidence":0.0-1.0}
	                                                  """;

	private const string ClassificationUserPromptTemplate = """
	                                                        Название канала: {0}
	                                                        Описание: {1}
	                                                        Последние посты (--- разделитель):
	                                                        {2}
	                                                        """;

	[DisableConcurrentExecution(100000)]
	public async Task ClassifyChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		if (!await ParseLock.WaitAsync(0, ct))
		{
			logger.LogWarning("Классификация уже выполняется, повторный запуск пропущен");
			return;
		}

		try
		{
			if (string.IsNullOrWhiteSpace(options.SecretKey))
			{
				logger.LogWarning("ClassificationApiKey не задан, пропускаем классификацию каналов");
				return;
			}

			var channels = await storage.GetUnclassifiedChannelsAsync(BatchSize, ct);
			if (channels.Count == 0)
			{
				return;
			}

			logger.LogInformation("Начинаем классификацию {Count} каналов", channels.Count);

			var sessionId = await storage.GetSessionIdByPurposeAsync(TelegramSessionPurpose.Classification, ct);

			if (sessionId is null)
			{
				logger.LogWarning("Нет активной сессии Classification ");
				return;
			}

			var telegramClient = await authService.GetClientAsync(sessionId!.Value, ct);
			if (telegramClient is null)
				return;

			foreach (var channel in channels)
			{
				try
				{
					await ClassifyChannelAsync(channel, telegramClient, ct);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Ошибка при классификации канала {ChannelId}", channel.Id);
				}
			}
		}
		finally
		{
			ParseLock.Release();
		}
	}

	private async Task ClassifyChannelAsync(
		ChannelForClassificationDto channel,
		WTelegram.Client telegramClient,
		CancellationToken ct
	)
	{
		var resolved = await ResolvePeerAsync(telegramClient, channel, ct);
		if (resolved.Peer is null)
		{
			logger.LogDebug("Не удалось найти канал {ChannelId} в Telegram для загрузки сообщений", channel.Id);
			return;
		}

		var sample = await FetchRecentMessagesAsync(telegramClient, resolved.Peer, ct);


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

		var messages = new List<ChatMessage>
		{
			new() { Role = "system", Content = ClassificationSystemPrompt },
			new() { Role = "user", Content = contentParts }
		};

		var response = await openRouterClient.SendMessageRawAsync(
			options.SecretKey!,
			options.Model,
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

		logger.LogInformation(
			"Канал {Channel} классифицирован: {Category}/{Subcategory} (confidence: {Confidence})",
			channel.Title, classification.Category, classification.Subcategory, classification.Confidence);
	}

	private async Task<RecentMessagesSample> FetchRecentMessagesAsync(
		WTelegram.Client client,
		InputPeer peer,
		CancellationToken ct
	)
	{
		var historyResult = await tgMessages.GetHistoryAsync(
			client, peer, limit: options.MessageSampleCount, ct: ct);

		if (!historyResult.IsSuccess)
		{
			logger.LogDebug("Не удалось получить сообщения канала {ChannelId}: {Status} {Error}",
				peer.ID, historyResult.Status, historyResult.ErrorMessage);
			return new RecentMessagesSample([], []);
		}

		var texts = new List<string>();
		var photos = new List<PhotoForClassification>();

		foreach (var message in historyResult.Value!.Messages.OfType<Message>())
		{
			if (!string.IsNullOrWhiteSpace(message.message))
			{
				var cleaned = CleanMessageText(message.message);
				if (!string.IsNullOrWhiteSpace(cleaned))
					texts.Add(TruncateMessage(cleaned, MaxTextLength));
			}

			if (photos.Count < MaxPhotoCount
			    && message is { media: MessageMediaPhoto { photo: Photo photo } })
			{
				photos.Add(new PhotoForClassification(message.ID, photo));
			}
		}

		logger.LogDebug("Загружено {TextCount} сообщений и {PhotoCount} фото для канала {ChannelId}",
			texts.Count, photos.Count, peer.ID);
		return new RecentMessagesSample(texts, photos);
	}

	private async Task<List<string>> DownloadAndPrepareImagesAsync(
		WTelegram.Client client,
		InputChannel channel,
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
				var downloadResult = await tgMessages.DownloadPhotoAsync(
					client, channel, item.MessageId, item.Photo, rawStream, ct);

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

	private async Task<ResolvedPeer> ResolvePeerAsync(
		WTelegram.Client client,
		ChannelForClassificationDto channel,
		CancellationToken ct
	)
	{
		if (!string.IsNullOrEmpty(channel.Username))
		{
			var resolved = await tgMessages.ResolveChannelAsync(client, channel.Username, ct);
			if (await resolved.HandleChannelUnavailableAsync(async () =>
				    await storage.MarkChannelBannedAsync(channel.Id, ct)))
				return new ResolvedPeer(null, null);

			if (!resolved.IsSuccess)
				return new ResolvedPeer(null, null);

			var tg = resolved.Value!;
			return new ResolvedPeer(
				new InputPeerChannel(tg.ID, tg.access_hash),
				new InputChannel(tg.ID, tg.access_hash));
		}

		if (channel.TelegramId.HasValue)
		{
			var dialogsResult = await tgMessages.GetAllDialogsAsync(client, ct);
			if (!dialogsResult.IsSuccess)
				return new ResolvedPeer(null, null);

			if (dialogsResult.Value!.chats.TryGetValue(channel.TelegramId.Value, out var chat))
			{
				if (chat is Channel tgChannel)
				{
					return new ResolvedPeer(
						new InputPeerChannel(tgChannel.ID, tgChannel.access_hash),
						new InputChannel(tgChannel.ID, tgChannel.access_hash));
				}

				return new ResolvedPeer(new InputPeerChat(chat.ID), null);
			}
		}

		return new ResolvedPeer(null, null);
	}

	private static string TruncateMessage(string text, int maxLength)
	{
		if (text.Length <= maxLength)
			return text;
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
				json = json[startIndex..(endIndex + 1)];
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

	private sealed record PhotoForClassification(int MessageId, Photo Photo);

	private sealed record ResolvedPeer(InputPeer? Peer, InputChannel? Channel);
}
