using MediatR;
using Security.IdentityServices;
using TgPoster.Telegram;
using TgPoster.Exceptions;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.SendPassword;

internal sealed class SendPasswordUseCase(
	ITelegramAuthService authService,
	ISendPasswordStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<SendPasswordCommand, SendPasswordResponse>
{
	public async Task<SendPasswordResponse> Handle(SendPasswordCommand request, CancellationToken ct)
	{
		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (!exists)
		{
			throw new TelegramSessionEntityNotFoundException(request.SessionId);
		}

		var success = await authService.SendPasswordAsync(request.SessionId, request.Password, ct);

		var message = success
			? "Авторизация успешно завершена"
			: "Неверный пароль";

		return new SendPasswordResponse(success, message);
	}
}