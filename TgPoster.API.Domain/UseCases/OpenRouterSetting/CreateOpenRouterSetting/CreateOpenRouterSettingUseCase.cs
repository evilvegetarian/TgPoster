using MediatR;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.OpenRouter;
using TgPoster.API.Domain.ConfigModels;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;

public class CreateOpenRouterSettingUseCase(
	OpenRouterOptions options,
	ICreateOpenRouterSettingStorage storage,
	ICryptoAES cryptoAes,
	IIdentityProvider provider,
	IOpenRouterClient openRouterClient
) : IRequestHandler<CreateOpenRouterSettingCommand, CreateOpenRouterSettingResponse>
{
	public async Task<CreateOpenRouterSettingResponse> Handle(
		CreateOpenRouterSettingCommand request,
		CancellationToken cancellationToken
	)
	{
		var userId = provider.Current.UserId;
		var response = await openRouterClient.SendMessageAsync(
			request.Token,
			request.Model,
			"Работаешь?",
			cancellationToken);
		var tokenEncrypted = cryptoAes.Encrypt(options.SecretKey, request.Token);
		var id = await storage.Create(tokenEncrypted, request.Model, userId, cancellationToken);
		return new CreateOpenRouterSettingResponse(id);
	}
}