using MediatR;
using Security.IdentityServices;
using Shared.Exceptions;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;

public class GetOpenRouterSettingUseCase(IGetOpenRouterSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<GetOpenRouterSettingQuery, OpenRouterSettingResponse>
{
	public async Task<OpenRouterSettingResponse> Handle(GetOpenRouterSettingQuery query, CancellationToken ctx)
	{
		var settings = await storage.Get(query.Id, provider.Current.UserId, ctx);
		if (settings is null)
		{
			throw new OpenRouterSettingNotFoundException();
		}

		return new OpenRouterSettingResponse
		{
			Model = settings.Model,
			Id = settings.Id
		};
	}
}