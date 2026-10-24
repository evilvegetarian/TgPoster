using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using Shared.Utilities;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

internal class EditMessageUseCase(
	IEditMessageStorage storage,
	IIdentityProvider provider,
	TelegramTokenService tokenService,
	ITelegramService telegramService,
	TelegramBotManager botManager,
	IPublishedMessageService publishedMessageService,
	TimeProvider timeProvider)
	: IRequestHandler<EditMessageCommand>
{
	public async Task Handle(EditMessageCommand request, CancellationToken ct)
	{
		var userId = provider.Current.UserId;
		var message = await storage.GetMessageAsync(request.Id, userId, ct);
		if (message is null)
		{
			throw new MessageNotFoundException(request.Id);
		}

		if (message.IsPublished)
		{
			await EditPublishedAsync(request, message, ct);
			return;
		}

		if (request.TimePosting < timeProvider.GetUtcNow())
		{
			throw new TimePostingInPastException();
		}

		var (token, chatId) = await tokenService.GetTokenByScheduleIdAsync(request.ScheduleId, ct);

		var bot = botManager.GetClient(token);
		var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.NewFiles, chatId, ct);

		await storage.UpdateMessageAsync(request, files, ct);
	}

	/// <summary>
	/// Правит опубликованный пост: медиа и расписание не меняются, новый текст уходит в канал
	/// </summary>
	/// <param name="request"></param>
	/// <param name="message"></param>
	/// <param name="ct"></param>
	private async Task EditPublishedAsync(EditMessageCommand request, EditMessageInfo message, CancellationToken ct)
	{
		if (request.ScheduleId != message.ScheduleId
		    || request.NewFiles.Count > 0
		    || !request.Files.SequenceEqual(message.FileIds))
		{
			throw new PublishedMessageMediaChangeException();
		}

		if (IsTextChanged(request.Text, message.Text))
		{
			await EditTextInChannelAsync(request.Text, message, ct);
		}

		// Пост уже вышел, поэтому время публикации остаётся тем, с которым он ушёл в канал
		await storage.UpdateMessageAsync(request with { TimePosting = message.TimePosting }, [], ct);
	}

	/// <summary>
	/// Меняет текст поста в канале: подпись первого сообщения альбома или текстовое сообщение
	/// </summary>
	/// <param name="text"></param>
	/// <param name="message"></param>
	/// <param name="ct"></param>
	private async Task EditTextInChannelAsync(string? text, EditMessageInfo message, CancellationToken ct)
	{
		if (message.TelegramTextMessageId is not { } textMessageId)
		{
			throw new PublishedMessageTextNotEditableException();
		}

		var isCaption = message.FileIds.Count > 0
		                && message.TelegramMessageIds is { Length: > 0 } ids
		                && ids[0] == textMessageId;
		var limit = isCaption ? TelegramLimits.CaptionLength : TelegramLimits.MessageLength;

		// Лимит не передаётся в Compose: молча обрезанный текст в канале хуже понятной ошибки
		var html = PostSignatureComposer.Compose(text, message.Signature, int.MaxValue);
		if (html.Length > limit)
		{
			throw new PublishedMessageTextTooLongException(limit);
		}

		var (token, _) = await tokenService.GetTokenByScheduleIdAsync(message.ScheduleId, ct);
		var result = await publishedMessageService.EditTextAsync(token, message.ChannelId, textMessageId, isCaption,
			html, ct);
		if (!result.IsSuccess)
		{
			throw new PublishedMessageEditFailedException(result.Error);
		}
	}

	/// <summary>
	/// Сравнивает тексты без учёта переводов строк: multipart-форма браузера превращает \n в \r\n
	/// </summary>
	/// <param name="newText"></param>
	/// <param name="currentText"></param>
	/// <returns></returns>
	private static bool IsTextChanged(string? newText, string? currentText) =>
		!string.Equals(
			(newText ?? string.Empty).ReplaceLineEndings("\n"),
			(currentText ?? string.Empty).ReplaceLineEndings("\n"),
			StringComparison.Ordinal);
}
