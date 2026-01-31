using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.DeleteTelegramSession;

internal sealed class DeleteTelegramSessionUseCase(
	IDeleteTelegramSessionStorage storage,
	TelegramClientManager clientManager,
	IIdentityProvider identityProvider
) : IRequestHandler<DeleteTelegramSessionCommand>
{
	public async Task Handle(DeleteTelegramSessionCommand request, CancellationToken ct)
	{
		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (!exists)
		{
			throw new TelegramSessionNotFoundException(request.SessionId);
		}

		await clientManager.RemoveActiveClientAsync(request.SessionId);
		await storage.DeleteAsync(request.SessionId, ct);
	}
}