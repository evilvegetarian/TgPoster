using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.OpenRouter;
using TgPoster.Worker.Domain.ConfigModels;

namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

internal sealed class ClassifyChannelWorker(
	IOpenRouterClient openRouterClient,
	IClassifyChannelStorage storage,
	ClassificationOptions options,
	ILogger<ClassifyChannelWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int BatchSize = 10;

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

		foreach (var channel in channels)
		{
			try
			{
				await ClassifyChannelAsync(channel, ct);
				await Task.Delay(TimeSpan.FromSeconds(5), ct);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Ошибка при классификации канала {ChannelId}", channel.Id);
			}
		}
	}

	private async Task ClassifyChannelAsync(ChannelForClassificationDto channel, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(channel.Title) && string.IsNullOrWhiteSpace(channel.Description))
		{
			logger.LogWarning("Канал {ChannelId} не имеет названия и описания, пропускаем", channel.Id);
			return;
		}

		logger.LogInformation("Классифицируем канал: {Title} ({ChannelId})", channel.Title, channel.Id);

		var prompt = string.Format(ClassificationPrompt, channel.Title ?? "", channel.Description ?? "");
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
