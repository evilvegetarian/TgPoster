using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.VerifyCode;

internal sealed class VerifyCodeUseCase(
	ITelegramAuthService authService,
	IVerifyCodeStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<VerifyCodeCommand, VerifyCodeResponse>
{
	public async Task<VerifyCodeResponse> Handle(VerifyCodeCommand request, CancellationToken ct)
	{
		var exists = await storage.ExistsAsync(identityProvider.Current.UserId, request.SessionId, ct);

		if (!exists)
		{
			throw new TelegramSessionEntityNotFoundException(request.SessionId);
		}

		var result = await authService.VerifyCodeAsync(request.SessionId, request.Code, ct);

		return new VerifyCodeResponse(result.Success, result.RequiresPassword, result.Message);
	}
}