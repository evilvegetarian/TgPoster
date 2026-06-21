using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.Proxies.DeleteProxy;

internal sealed class DeleteProxyUseCase(
	IDeleteProxyStorage storage,
	IIdentityProvider identityProvider,
	ITelegramAuthService authService
) : IRequestHandler<DeleteProxyCommand>
{
	public async Task Handle(DeleteProxyCommand request, CancellationToken ct)
	{
		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.Id, ct);
		if (!exists)
		{
			throw new ProxyNotFoundException(request.Id);
		}

		var affectedSessions = await storage.DeleteAsync(request.Id, ct);

		foreach (var sessionId in affectedSessions)
		{
			await authService.RemoveClientAsync(sessionId);
		}
	}
}