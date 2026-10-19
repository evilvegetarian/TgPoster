using MediatR;
using Shared.Enums;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

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

		// Чат пришёл недоступным для аккаунта: тот же бан, что и при неудачной пересылке, его видит вся автоматика.
		// Доступный чат бан не снимает: пересылка может падать и в видимый чат, снимает только включение направления
		var isAccountBanned = result.ChatStatus == ChatStatus.Banned;
		if (isAccountBanned)
		{
			await storage.AddAccountBanAsync(
				sessionId.Value,
				TelegramChatId.ResolveRaw(chatId.Value),
				"Telegram отдал чат как недоступный аккаунту",
				ct);
		}

		var avatarBase64 = result.AvatarThumbnail != null
			? "data:image/jpeg;base64," + Convert.ToBase64String(result.AvatarThumbnail)
			: null;

		await storage.UpdateDestinationInfoAsync(
			request.DestinationId,
			result.Title,
			result.Username,
			result.MemberCount,
			result.ChatType,
			// Бан уже в реестре, в направлении хранится только членство — его при бане не трогаем
			isAccountBanned ? null : result.ChatStatus,
			avatarBase64,
			ct);
	}
}