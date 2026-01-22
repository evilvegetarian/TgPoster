using MediatR;
using Security.Cryptography;
using Shared.OpenRouter;
using Shared.OpenRouter.Models.Request;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;

public record GenerateAiContentCommand(Guid MessageId) : IRequest<GenerateAiContentResponse>;

internal class GenerateAiContentHandler(
	IGenerateAiContentStorage storage,
	IOpenRouterClient client,
	ICryptoAES crypto,
	TelegramService service,
	TelegramTokenService tokenService,
	OpenRouterOptions options)
	: IRequestHandler<GenerateAiContentCommand, GenerateAiContentResponse>
{
	public async Task<GenerateAiContentResponse> Handle(GenerateAiContentCommand request, CancellationToken ct)
	{
		var openApiToken = await storage.GetOpenRouterAsync(request.MessageId, ct);
		if (openApiToken is null)
		{
			throw new ApplicationException("Токен не найден");
		}

		var token = crypto.Decrypt(options.SecretKey, openApiToken.Token);
		var messageDto = await storage.GetMessageAsync(request.MessageId, ct);
		if (messageDto is null)
		{
			throw new MessageNotFoundException(request.MessageId);
		}

		var tk = await tokenService.GetTokenByScheduleIdAsync(openApiToken.ScheduleId, ct);
		var bot = new TelegramBotClient(tk.token);
		var prompt = await storage.GetPromptSettingsAsync(openApiToken.ScheduleId, ct);
		var contentParts = new List<MessageContentPart>
		{
			new()
			{
				Text =
					"Ты должен мне помочь создать текстовое описание для поста. Используй тот язык, который используется ниже."
					+ " Также возможно будет приложено медиа. Если у сообщения подпись Фото, это означает что изначально медиа было одной фотографией."
					+ " Если подпись Видео 1, или Видео 2 или Видео 3. Это подпись к фото, которые являются превью видео, но переданы как фото. Причем цифра означает какое именно видео."
					+ " На выходе должно быть только новое текстовое сообщение на основе всех данных что я предоставил тебе, без каких либо других данных только пост и все",
				Type = "text"
			},
			new() { Type = "text", Text = messageDto.TextMessage }
		};
		if (prompt?.PhotoPrompt is not null)
		{
			contentParts.Add(new MessageContentPart
			{
				Type = "text",
				Text = "Это промпт для Фото: " + prompt.PhotoPrompt
			});
		}

		if (prompt?.VideoPrompt is not null)
		{
			contentParts.Add(new MessageContentPart
			{
				Type = "text",
				Text = "Это промпт для Видео: " + prompt.VideoPrompt
			});
		}

		if (prompt?.TextPrompt is not null)
		{
			contentParts.Add(new MessageContentPart
			{
				Type = "text",
				Text = "Это промпт для Текста: " + prompt.TextPrompt
			});
		}

		var ss = 1;

		foreach (var file in messageDto.Files)
		{
			if (file.Previews.Count > 0)
			{
				foreach (var preview in file.Previews)
				{
					var bytes = await service.GetByteFileAsync(bot, preview.TgFileId, ct);
					contentParts.Add(new MessageContentPart
						{
							Text = "Видео " + ss,
							Type = "image_url",
							ImageUrl = new ImageUrlInfo
							{
								Url = client.ToLocalImageDataUrl(bytes)
							}
						}
					);
				}

				ss++;
			}
			else
			{
				var bytes = await service.GetByteFileAsync(bot, file.TgFileId, ct);
				contentParts.Add(new MessageContentPart
					{
						Text = "Фото",
						Type = "image_url",
						ImageUrl = new ImageUrlInfo
						{
							Url = client.ToLocalImageDataUrl(bytes)
						}
					}
				);
			}
		}

		var message = new ChatMessage
		{
			Role = "user",
			Content = contentParts
		};

		var response = await client.SendMessageRawAsync(token, openApiToken.Model, [message], ct);

		return new GenerateAiContentResponse
		{
			Content = string.Join(' ', response.Choices.Select(x => x.Message.Content))
		};
	}
}