using MediatR;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;
using TL;

namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

internal sealed class AddRepostDestinationUseCase(
	IAddRepostDestinationStorage storage,
	TelegramAuthService authService,
	TelegramChatService chatService)
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

		var destinationId = await storage.AddDestinationAsync(
			request.RepostSettingsId,
			info.Id,
			ct);

		return new AddRepostDestinationResponse { Id = destinationId };
	}
}