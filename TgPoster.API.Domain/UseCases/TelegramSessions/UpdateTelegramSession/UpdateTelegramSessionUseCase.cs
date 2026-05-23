using MediatR;
using Security.IdentityServices;
using TgPoster.Telegram;
using TgPoster.Exceptions;
using TgPoster.API.Domain.UseCases.Proxies.ListProxies;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.UpdateTelegramSession;

internal sealed class UpdateTelegramSessionUseCase(
	IUpdateTelegramSessionStorage storage,
	IListProxiesStorage proxyStorage,
	ITelegramAuthService authService,
	IIdentityProvider identityProvider
) : IRequestHandler<UpdateTelegramSessionCommand>
{
	public async Task Handle(UpdateTelegramSessionCommand request, CancellationToken ct)
	{
		var session = await storage.GetByIdAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (session == null)
		{
			throw new TelegramSessionEntityNotFoundException(request.SessionId);
		}

		if (request.ProxyId.HasValue)
		{
			var ownsProxy = await proxyStorage.BelongsToUserAsync(
				identityProvider.Current.UserId, request.ProxyId.Value, ct);
			if (!ownsProxy)
				throw new ProxyNotFoundException(request.ProxyId.Value);
		}

		var proxyChanged = session.ProxyId != request.ProxyId;

		if (request.IsActive == false || proxyChanged)
		{
			await authService.RemoveClientAsync(request.SessionId);
		}

		await storage.UpdateAsync(request.SessionId, request.Name, request.IsActive, request.ProxyId, ct);
	}
}