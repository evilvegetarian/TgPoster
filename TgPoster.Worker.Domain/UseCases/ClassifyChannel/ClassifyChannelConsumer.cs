using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.OpenRouter;
using TgPoster.Worker.Domain.ConfigModels;

namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

internal sealed class ClassifyChannelConsumer(
	IOpenRouterClient openRouterClient,
	IClassifyChannelStorage storage,
	ClassificationOptions options,
	ILogger<ClassifyChannelConsumer> logger) : IConsumer<ClassifyChannelContract>
{
	private const string ClassificationPrompt = """
		Ты — классификатор Telegram-каналов. Определи основную тематику канала по его названию и описанию.

		Название: {0}
		Описание: {1}

		Ответь строго в JSON формате без markdown:
		{{
		  "category": "одна из: Технологии, Новости, Крипто, Бизнес, Маркетинг, Развлечения, Образование, Политика, Спорт, Здоровье, Путешествия, Еда, Музыка, Игры, Авто, Финансы, Наука, Дизайн, Юмор, 18+, Другое",
		  "subcategory": "уточнение тематики",
		  "tags": ["тег1", "тег2", "тег3"],
		  "language": "код языка (ru, en, uk и т.д.)",
		  "confidence": 0.0-1.0
		}}
		""";

	public async Task Consume(ConsumeContext<ClassifyChannelContract> context)
	{
		var channelId = context.Message.ChannelId;
		var ct = context.CancellationToken;

		if (string.IsNullOrWhiteSpace(options.ApiKey))
		{
			logger.LogWarning("ClassificationApiKey не задан, пропускаем классификацию канала {ChannelId}", channelId);
			return;
		}

		var channel = await storage.GetChannelForClassificationAsync(channelId, ct);
		if (channel is null)
		{
			logger.LogWarning("Канал {ChannelId} не найден для классификации", channelId);
			return;
		}

		if (string.IsNullOrWhiteSpace(channel.Title) && string.IsNullOrWhiteSpace(channel.Description))
		{
			logger.LogWarning("Канал {ChannelId} не имеет названия и описания, пропускаем", channelId);
			return;
		}

		logger.LogInformation("Классифицируем канал: {Title} ({ChannelId})", channel.Title, channelId);

		var prompt = string.Format(ClassificationPrompt, channel.Title ?? "", channel.Description ?? "");
		var response = await openRouterClient.SendMessageAsync(
			options.ApiKey,
			options.Model,
			prompt,
			ct);

		var content = response.Choices.FirstOrDefault()?.Message.Content?.ToString();
		if (string.IsNullOrWhiteSpace(content))
		{
			logger.LogWarning("Пустой ответ от LLM для канала {ChannelId}", channelId);
			return;
		}

		var classification = ParseClassification(content);
		if (classification is null)
		{
			logger.LogWarning("Не удалось распарсить ответ LLM для канала {ChannelId}: {Content}", channelId, content);
			return;
		}

		await storage.UpdateClassificationAsync(
			channelId,
			classification.Category,
			classification.Subcategory,
			classification.Tags,
			classification.Language,
			classification.Confidence,
			ct);

		logger.LogInformation(
			"Канал {ChannelId} классифицирован: {Category}/{Subcategory} (confidence: {Confidence})",
			channelId, classification.Category, classification.Subcategory, classification.Confidence);
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
