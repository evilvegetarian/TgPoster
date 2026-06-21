using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.DeleteTelegramSession;

internal sealed class DeleteTelegramSessionUseCase(
	IDeleteTelegramSessionStorage storage,
	ITelegramAuthService authService,
	IIdentityProvider identityProvider
) : IRequestHandler<DeleteTelegramSessionCommand>
{
	public async Task Handle(DeleteTelegramSessionCommand request, CancellationToken ct)
	{
		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (!exists)
		{
			throw new TelegramSessionEntityNotFoundException(request.SessionId);
		}

		await authService.RemoveClientAsync(request.SessionId);
		await storage.DeleteAsync(request.SessionId, ct);
	}
}