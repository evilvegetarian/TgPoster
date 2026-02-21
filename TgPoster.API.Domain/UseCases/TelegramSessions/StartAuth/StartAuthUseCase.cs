using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.StartAuth;

internal sealed class StartAuthUseCase(
	ITelegramAuthService authService,
	IStartAuthStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<StartAuthCommand, StartAuthResponse>
{
	public async Task<StartAuthResponse> Handle(StartAuthCommand request, CancellationToken ct)
	{
		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (!exists)
		{
			throw new TelegramSessionNotFoundException(request.SessionId);
		}

		var status = await authService.StartAuthAsync(request.SessionId, ct);

		var message = status switch
		{
			"code_sent" => "Код верификации отправлен в Telegram",
			"already_authorized" => "Сессия уже авторизована",
			_ => $"Неожиданный статус: {status}"
		};

		return new StartAuthResponse(status, message);
	}
}