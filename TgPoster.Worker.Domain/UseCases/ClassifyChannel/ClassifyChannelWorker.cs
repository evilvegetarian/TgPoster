using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.OpenRouter;
using Shared.Telegram;
using TL;
using TgPoster.Worker.Domain.ConfigModels;
using Message = TL.Message;

namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

internal sealed class ClassifyChannelWorker(
	IOpenRouterClient openRouterClient,
	IClassifyChannelStorage storage,
	ITelegramAuthService authService,
	ITelegramMessageService tgMessages,
	ClassificationOptions options,
	ILogger<ClassifyChannelWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int BatchSize = 10;

	private const string ClassificationPrompt = """
	                                            Ты — классификатор Telegram-каналов. Определи основную тематику канала по его названию, описанию и последним постам.

	                                            Название: {0}
	                                            Описание: {1}
	                                            Последние посты:
	                                            {2}

	                                            Ответь строго в JSON формате без markdown:
	                                            {{
	                                              "category": "одна из: Технологии, Новости, Крипто, Бизнес, Маркетинг, Развлечения, Образование, Политика, Спорт, Здоровье, Путешествия, Еда, Музыка, Игры, Авто, Финансы, Наука, Дизайн, Юмор, 18+, Другое",
	                                              "subcategory": "уточнение тематики",
	                                              "tags": ["тег1", "тег2", "тег3"],
	                                              "language": "код языка (ru, en, uk и т.д.)",
	                                              "confidence": 0.0-1.0
	                                            }}
	                                            """;

	[DisableConcurrentExecution(100000)]
	public async Task ClassifyChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		if (string.IsNullOrWhiteSpace(options.ApiKey))
		{
			logger.LogWarning("ClassificationApiKey не задан, пропускаем классификацию каналов");
			return;
		}

		var channels = await storage.GetUnclassifiedChannelsAsync(BatchSize, ct);
		if (channels.Count == 0)
		{
			logger.LogDebug("Нет каналов для классификации");
			return;
		}

		logger.LogInformation("Начинаем классификацию {Count} каналов", channels.Count);

		var sessionId = await storage.GetSessionIdByPurposeAsync(TelegramSessionPurpose.Classification, ct);

		if (sessionId is null)
			logger.LogWarning("Нет активной сессии Classification, классификация будет только по заголовку и описанию");

		var telegramClient = await authService.GetClientAsync(sessionId!.Value, ct);

		foreach (var channel in channels)
		{
			try
			{
				await ClassifyChannelAsync(channel, telegramClient, ct);
				await Task.Delay(TimeSpan.FromSeconds(5), ct);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Ошибка при классификации канала {ChannelId}", channel.Id);
			}
		}
	}

	private async Task ClassifyChannelAsync(
		ChannelForClassificationDto channel,
		WTelegram.Client? telegramClient,
		CancellationToken ct
	)
	{
		if (string.IsNullOrWhiteSpace(channel.Title) && string.IsNullOrWhiteSpace(channel.Description))
		{
			logger.LogWarning("Канал {ChannelId} не имеет названия и описания, пропускаем", channel.Id);
			return;
		}

		logger.LogInformation("Классифицируем канал: {Title} ({ChannelId})", channel.Title, channel.Id);

		var recentMessages = telegramClient is not null
			? await FetchRecentMessagesAsync(telegramClient, channel, ct)
			: [];

		var messagesSection = recentMessages.Count > 0
			? string.Join("\n---\n", recentMessages)
			: "(нет данных)";

		var prompt = string.Format(ClassificationPrompt, channel.Title ?? "", channel.Description ?? "",
			messagesSection);
		var response = await openRouterClient.SendMessageAsync(
			options.ApiKey!,
			options.Model,
			prompt,
			ct);

		var content = response.Choices.FirstOrDefault()?.Message.Content?.ToString();
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
			"Канал {ChannelId} классифицирован: {Category}/{Subcategory} (confidence: {Confidence})",
			channel.Id, classification.Category, classification.Subcategory, classification.Confidence);
	}

	private async Task<List<string>> FetchRecentMessagesAsync(
		WTelegram.Client client,
		ChannelForClassificationDto channel,
		CancellationToken ct
	)
	{
		var peer = await ResolvePeerAsync(client, channel, ct);
		if (peer is null)
		{
			logger.LogDebug("Не удалось найти канал {ChannelId} в Telegram для загрузки сообщений", channel.Id);
			return [];
		}

		var historyResult = await tgMessages.GetHistoryAsync(
			client, peer, limit: options.MessageSampleCount, ct: ct);

		if (!historyResult.IsSuccess)
		{
			logger.LogDebug("Не удалось получить сообщения канала {ChannelId}: {Status} {Error}",
				channel.Id, historyResult.Status, historyResult.ErrorMessage);
			return [];
		}

		var messages = historyResult.Value!.Messages
			.OfType<Message>()
			.Where(m => !string.IsNullOrWhiteSpace(m.message))
			.Select(m => TruncateMessage(m.message, 500))
			.ToList();

		logger.LogDebug("Загружено {Count} сообщений для канала {ChannelId}", messages.Count, channel.Id);
		return messages;
	}

	private async Task<InputPeer?> ResolvePeerAsync(
		WTelegram.Client client,
		ChannelForClassificationDto channel,
		CancellationToken ct
	)
	{
		if (!string.IsNullOrEmpty(channel.Username))
		{
			var resolved = await tgMessages.ResolveChannelAsync(client, channel.Username, ct);
			return resolved.IsSuccess
				? new InputPeerChannel(resolved.Value!.ID, resolved.Value.access_hash)
				: null;
		}

		if (channel.TelegramId.HasValue)
		{
			var dialogsResult = await tgMessages.GetAllDialogsAsync(client, ct);
			if (!dialogsResult.IsSuccess)
				return null;

			if (dialogsResult.Value!.chats.TryGetValue(channel.TelegramId.Value, out var chat))
			{
				return chat is Channel tgChannel
					? new InputPeerChannel(tgChannel.ID, tgChannel.access_hash)
					: new InputPeerChat(chat.ID);
			}
		}

		return null;
	}

	private static string TruncateMessage(string text, int maxLength)
	{
		if (text.Length <= maxLength)
			return text;
		return string.Concat(text.AsSpan(0, maxLength), "...");
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
}