using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.UpdateTelegramSession;

internal sealed class UpdateTelegramSessionUseCase(
	IUpdateTelegramSessionStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<UpdateTelegramSessionCommand>
{
	public async Task Handle(UpdateTelegramSessionCommand request, CancellationToken ct)
	{
		var session = await storage.GetByIdAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (session == null)
		{
			throw new TelegramSessionNotFoundException(request.SessionId);
		}

		await storage.UpdateAsync(request.SessionId, request.Name, request.IsActive, ct);
	}
}