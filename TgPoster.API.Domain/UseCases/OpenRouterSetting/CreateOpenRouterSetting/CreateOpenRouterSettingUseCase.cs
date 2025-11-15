using System.ComponentModel.DataAnnotations;
using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;

public class CreateOpenRouterSettingUseCase(
	OpenRouterOptions options,
	ICreateOpenRouterSettingStorage storage,
	ICryptoAES cryptoAes,
	IIdentityProvider provider,
	OpenRouterClient openRouterClient
) : IRequestHandler<CreateOpenRouterSettingCommand, CreateOpenRouterSettingResponse>
{
	public async Task<CreateOpenRouterSettingResponse> Handle(
		CreateOpenRouterSettingCommand request,
		CancellationToken cancellationToken
	)
	{
		var userId = provider.Current.UserId;
		var response = await openRouterClient.SendMessageAsync(request.Token, "Работаешь?", request.Model);
		var tokenEncrypted = cryptoAes.Encrypt(options.SecretKey, request.Token);
		var id = await storage.Create(tokenEncrypted, request.Model, userId, cancellationToken);
		return new CreateOpenRouterSettingResponse(id);
	}
}