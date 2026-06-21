using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;

internal sealed class CreateCommentRepostUseCase(
	ICreateCommentRepostStorage storage,
	ITelegramChatService chatService,
	IIdentityProvider identity)
	: IRequestHandler<CreateCommentRepostCommand, CreateCommentRepostResponse>
{
	public async Task<CreateCommentRepostResponse> Handle(CreateCommentRepostCommand request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistsAsync(request.ScheduleId, identity.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		if (!await storage.TelegramSessionExistsAndActiveAsync(request.TelegramSessionId, ct))
		{
			throw new TelegramSessionEntityNotFoundException(request.TelegramSessionId);
		}

		var chatInfo = await chatService.GetChatInfoAsync(request.TelegramSessionId, request.WatchedChannel);
		var sourceChannel = await storage.GetSourceChannelAsync(request.ScheduleId, ct);

		// Проверка что вступил в канал откуда будет репост
		await chatService.GetChatInfoAsync(request.TelegramSessionId, sourceChannel);

		if (await storage.SettingsExistsAsync(chatInfo.Id, request.ScheduleId, ct))
		{
			throw new CommentRepostSettingsAlreadyExistsException(request.WatchedChannel);
		}

		var (linkedChatId, discussionAccessHash) =
			await chatService.GetLinkedDiscussionGroupAsync(request.TelegramSessionId, chatInfo, ct);

		if (linkedChatId == 0)
		{
			throw new ChannelNoCommentsException(request.WatchedChannel);
		}

		var settingsId = await storage.CreateAsync(
			request.WatchedChannel,
			chatInfo.Id,
			chatInfo.AccessHash,
			linkedChatId,
			discussionAccessHash,
			request.TelegramSessionId,
			request.ScheduleId,
			ct);

		return new CreateCommentRepostResponse { Id = settingsId };
	}
}