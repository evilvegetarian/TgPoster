using MediatR;
using Security.IdentityServices;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.SendPassword;

internal sealed class SendPasswordUseCase(
	TelegramAuthService authService,
	ISendPasswordStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<SendPasswordCommand, SendPasswordResponse>
{
	public async Task<SendPasswordResponse> Handle(SendPasswordCommand request, CancellationToken ct)
	{
		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (!exists)
		{
			throw new TelegramSessionNotFoundException(request.SessionId);
		}

		var success = await authService.SendPasswordAsync(request.SessionId, request.Password, ct);

		var message = success
			? "Авторизация успешно завершена"
			: "Неверный пароль";

		return new SendPasswordResponse(success, message);
	}
}