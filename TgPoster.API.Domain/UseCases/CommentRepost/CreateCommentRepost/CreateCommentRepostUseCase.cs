using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;
using TL;

namespace TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;

internal sealed class CreateCommentRepostUseCase(
	ICreateCommentRepostStorage storage,
	TelegramAuthService authService,
	TelegramChatService chatService,
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

		if (await storage.SettingsExistsAsync(chatInfo.Id, request.ScheduleId, ct))
			throw new CommentRepostSettingsAlreadyExistsException(request.WatchedChannel);

		var fullChannel = await client.Channels_GetFullChannel(
			new InputChannel(chatInfo.Id, chatInfo.AccessHash));

		if (fullChannel.full_chat is not ChannelFull channelFull || channelFull.linked_chat_id == 0)
			throw new ChannelNoCommentsException(request.WatchedChannel);

		long? discussionAccessHash = null;
		if (fullChannel.chats.TryGetValue(channelFull.linked_chat_id, out var discussionChat))
		{
			if (discussionChat is Channel dc)
				discussionAccessHash = dc.access_hash;
		}

		var settingsId = await storage.CreateAsync(
			request.WatchedChannel,
			chatInfo.Id,
			chatInfo.AccessHash,
			channelFull.linked_chat_id,
			discussionAccessHash,
			request.TelegramSessionId,
			request.ScheduleId,
			ct);

		return new CreateCommentRepostResponse { Id = settingsId };
	}
}
