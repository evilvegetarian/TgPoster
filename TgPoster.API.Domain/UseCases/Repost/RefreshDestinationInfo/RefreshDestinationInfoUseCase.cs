using MediatR;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.Repost.RefreshDestinationInfo;

internal sealed class RefreshDestinationInfoUseCase(
	IRefreshDestinationInfoStorage storage,
	ITelegramChatService chatService)
	: IRequestHandler<RefreshDestinationInfoCommand>
{
	public async Task Handle(RefreshDestinationInfoCommand request, CancellationToken ct)
	{
		var sessionId = await storage.GetTelegramSessionIdAsync(request.DestinationId, ct);
		if (sessionId == null)
		{
			throw new RepostDestinationNotFoundException(request.DestinationId);
		}

		var chatId = await storage.GetChatIdAsync(request.DestinationId, ct);
		if (chatId == null)
		{
			throw new RepostDestinationNotFoundException(request.DestinationId);
		}

		var result = await chatService.RefreshChannelInfoAsync(sessionId.Value, chatId.Value);

		var avatarBase64 = result.AvatarThumbnail != null
			? "data:image/jpeg;base64," + Convert.ToBase64String(result.AvatarThumbnail)
			: null;

		await storage.UpdateDestinationInfoAsync(
			request.DestinationId,
			result.Title,
			result.Username,
			result.MemberCount,
			result.ChatType,
			result.ChatStatus,
			avatarBase64,
			ct);
	}
}