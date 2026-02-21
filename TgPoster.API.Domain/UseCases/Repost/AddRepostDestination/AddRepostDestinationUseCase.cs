using MediatR;
using Shared.Enums;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

internal sealed class AddRepostDestinationUseCase(
	IAddRepostDestinationStorage storage,
	ITelegramAuthService authService,
	ITelegramChatService chatService)
	: IRequestHandler<AddRepostDestinationCommand, AddRepostDestinationResponse>
{
	public async Task<AddRepostDestinationResponse> Handle(AddRepostDestinationCommand request, CancellationToken ct)
	{
		var telegramSessionId = await storage.GetTelegramSessionIdAsync(request.RepostSettingsId, ct);
		if (telegramSessionId == null)
			throw new RepostSettingsNotFoundException(request.RepostSettingsId);

		// if (await storage.DestinationExistsAsync(request.RepostSettingsId, request.ChatIdentifier, ct))
		// 	throw new RepostDestinationAlreadyExistsException(request.ChatIdentifier);

		var client = await authService.GetClientAsync(telegramSessionId.Value, ct);

		var info = await chatService.GetChatInfoAsync(client, request.ChatIdentifier);
		var fullInfo = await chatService.GetFullChannelInfoAsync(client, info);

		var chatType = info.IsChannel ? ChatType.Channel
			: info.IsGroup ? ChatType.Group
			: ChatType.Unknown;

		var avatarBase64 = fullInfo.AvatarThumbnail != null
			? "data:image/jpeg;base64," + Convert.ToBase64String(fullInfo.AvatarThumbnail)
			: null;

		var destinationId = await storage.AddDestinationAsync(
			request.RepostSettingsId,
			info.Id,
			fullInfo.Title,
			fullInfo.Username,
			fullInfo.MemberCount,
			chatType,
			ChatStatus.Active,
			avatarBase64,
			ct);

		return new AddRepostDestinationResponse { Id = destinationId };
	}
}