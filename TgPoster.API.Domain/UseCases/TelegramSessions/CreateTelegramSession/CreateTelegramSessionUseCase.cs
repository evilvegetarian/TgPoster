using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Proxies.ListProxies;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;

internal sealed class CreateTelegramSessionUseCase(
	ICreateTelegramSessionStorage storage,
	IListProxiesStorage proxyStorage,
	IIdentityProvider provider,
	ITelegramAuthService authService
) : IRequestHandler<CreateTelegramSessionCommand, CreateTelegramSessionResponse>
{
	public async Task<CreateTelegramSessionResponse> Handle(
		CreateTelegramSessionCommand request,
		CancellationToken ct
	)
	{
		if (request.ProxyId.HasValue)
		{
			var ownsProxy = await proxyStorage.BelongsToUserAsync(
				provider.Current.UserId, request.ProxyId.Value, ct);
			if (!ownsProxy)
				throw new ProxyNotFoundException(request.ProxyId.Value);
		}

		var createResponse = await storage.CreateAsync(
			provider.Current.UserId,
			request.ApiId,
			request.ApiHash,
			request.PhoneNumber,
			request.Name,
			request.ProxyId,
			ct
		);

		var authStatus = await authService.StartAuthAsync(createResponse.Id, ct);

		return new CreateTelegramSessionResponse(
			createResponse.Id,
			createResponse.Name,
			createResponse.IsActive,
			authStatus
		);
	}
}