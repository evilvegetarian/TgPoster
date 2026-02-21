using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;

internal sealed class CreateCommentRepostUseCase(
	ICreateCommentRepostStorage storage,
	ITelegramAuthService authService,
	ITelegramChatService chatService,
	IIdentityProvider identity)
	: IRequestHandler<CreateCommentRepostCommand, CreateCommentRepostResponse>
{
	public async Task<CreateCommentRepostResponse> Handle(CreateCommentRepostCommand request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistsAsync(request.ScheduleId, identity.Current.UserId, ct))
			throw new ScheduleNotFoundException(request.ScheduleId);

		if (!await storage.TelegramSessionExistsAndActiveAsync(request.TelegramSessionId, ct))
			throw new TelegramSessionNotFoundException(request.TelegramSessionId);

		var client = await authService.GetClientAsync(request.TelegramSessionId, ct);
		var chatInfo = await chatService.GetChatInfoAsync(client, request.WatchedChannel);
		var sourceChannel = await storage.GetSourceChannelAsync(request.ScheduleId, ct);

		// Проверка что вступил в канал откуда будет репост
		await chatService.GetChatInfoAsync(client, sourceChannel);

		if (await storage.SettingsExistsAsync(chatInfo.Id, request.ScheduleId, ct))
			throw new CommentRepostSettingsAlreadyExistsException(request.WatchedChannel);

		var (linkedChatId, discussionAccessHash) =
			await chatService.GetLinkedDiscussionGroupAsync(client, chatInfo, ct);

		if (linkedChatId == 0)
			throw new ChannelNoCommentsException(request.WatchedChannel);

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
