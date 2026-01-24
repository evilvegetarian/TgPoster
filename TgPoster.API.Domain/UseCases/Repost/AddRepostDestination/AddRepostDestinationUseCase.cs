using MediatR;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;
using TL;

namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

internal sealed class AddRepostDestinationUseCase(
	IAddRepostDestinationStorage storage,
	TelegramAuthService authService)
	: IRequestHandler<AddRepostDestinationCommand, AddRepostDestinationResponse>
{
	public async Task<AddRepostDestinationResponse> Handle(AddRepostDestinationCommand request, CancellationToken ct)
	{
		var telegramSessionId = await storage.GetTelegramSessionIdAsync(request.RepostSettingsId, ct);
		if (telegramSessionId == null)
			throw new RepostSettingsNotFoundException(request.RepostSettingsId);

		if (await storage.DestinationExistsAsync(request.RepostSettingsId, request.ChatIdentifier, ct))
			throw new RepostDestinationAlreadyExistsException(request.ChatIdentifier);

		var client = await authService.GetClientAsync(telegramSessionId.Value, ct);

		try
		{
			var resolveResult = await client.Contacts_ResolveUsername(request.ChatIdentifier);

			if (resolveResult.Chat == null)
				throw new TelegramChannelNotFoundException(request.ChatIdentifier);
		}
		catch (Exception ex) when (ex is not TelegramChannelNotFoundException)
		{
			throw new TelegramChannelAccessException(request.ChatIdentifier, ex.Message);
		}

		var destinationId = await storage.AddDestinationAsync(
			request.RepostSettingsId,
			request.ChatIdentifier,
			ct);

		return new AddRepostDestinationResponse(destinationId);
	}
}
