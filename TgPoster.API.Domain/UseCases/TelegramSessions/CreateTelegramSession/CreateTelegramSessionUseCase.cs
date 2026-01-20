using MediatR;
using Security.Interfaces;
using Shared.Services;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;

internal sealed class CreateTelegramSessionUseCase(
	ICreateTelegramSessionStorage storage,
	IIdentityProvider provider,
	TelegramAuthService authService
) : IRequestHandler<CreateTelegramSessionCommand, CreateTelegramSessionResponse>
{
	public async Task<CreateTelegramSessionResponse> Handle(
		CreateTelegramSessionCommand request,
		CancellationToken ct
	)
	{
		var createResponse = await storage.CreateAsync(
			provider.Current.UserId,
			request.ApiId,
			request.ApiHash,
			request.PhoneNumber,
			request.Name,
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
