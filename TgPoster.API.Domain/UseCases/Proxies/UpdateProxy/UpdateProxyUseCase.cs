using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using TgPoster.Telegram;
using TgPoster.Exceptions;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Proxies.UpdateProxy;

internal sealed class UpdateProxyUseCase(
	IUpdateProxyStorage storage,
	IIdentityProvider identityProvider,
	ITelegramAuthService authService
) : IRequestHandler<UpdateProxyCommand>
{
	public async Task Handle(UpdateProxyCommand request, CancellationToken ct)
	{
		ProxyValidation.Validate(request.Type, request.Host, request.Port, request.Secret);

		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.Id, ct);
		if (!exists)
			throw new ProxyNotFoundException(request.Id);

		var affectedSessions = await storage.UpdateAsync(
			request.Id,
			request.Name,
			request.Type,
			request.Host,
			request.Port,
			NormalizeOptional(request.Username),
			NormalizeOptional(request.Password),
			request.Type == ProxyType.MTProxy ? NormalizeOptional(request.Secret) : null,
			ct);

		foreach (var sessionId in affectedSessions)
		{
			await authService.RemoveClientAsync(sessionId);
		}
	}

	private static string? NormalizeOptional(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value;
}
