using MediatR;
using Shared.Enums;
using TgPoster.Telegram;
using TgPoster.Exceptions;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

internal sealed class AddRepostDestinationUseCase(
	IAddRepostDestinationStorage storage,
	ITelegramChatService chatService)
	: IRequestHandler<AddRepostDestinationCommand, AddRepostDestinationResponse>
{
	public async Task<AddRepostDestinationResponse> Handle(AddRepostDestinationCommand request, CancellationToken ct)
	{
		var telegramSessionId = await storage.GetTelegramSessionIdAsync(request.RepostSettingsId, ct);
		if (telegramSessionId == null)
		{
			throw new RepostSettingsNotFoundException(request.RepostSettingsId);
		}

		var info = await chatService.GetChatInfoAsync(telegramSessionId.Value, request.ChatIdentifier);

		// Репост пересылает контент с медиа/видео, поэтому требуем права и на сообщения, и на медиа.
		// Иначе пересылка упадёт в рантайме с CHAT_SEND_*_FORBIDDEN
		if (!info.CanSendMessages)
		{
			throw new TelegramChatNoWritePermissionException(info.Title);
		}

		if (!info.CanSendMedia)
		{
			throw new TelegramChatNoMediaPermissionException(info.Title);
		}

		var fullInfo = await chatService.GetFullChannelInfoAsync(telegramSessionId.Value, info);

		var chatType = info.IsChannel ? ChatType.Channel
			: info.IsGroup ? ChatType.Group
			: ChatType.Unknown;

		var avatarBase64 = fullInfo.AvatarThumbnail != null
			? "data:image/jpeg;base64," + Convert.ToBase64String(fullInfo.AvatarThumbnail)
			: null;

		var result = await storage.AddDestinationAsync(
			request.RepostSettingsId,
			info.Id,
			fullInfo.Title,
			fullInfo.Username,
			fullInfo.MemberCount,
			chatType,
			ChatStatus.Active,
			avatarBase64,
			ct);

		return new AddRepostDestinationResponse
		{
			Id = result.DestinationId,
			DiscoveredChannelId = result.DiscoveredChannelId
		};
	}
}