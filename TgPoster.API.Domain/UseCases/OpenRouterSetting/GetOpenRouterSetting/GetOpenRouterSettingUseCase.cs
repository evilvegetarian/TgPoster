using MediatR;
using Security.Interfaces;
using Shared.SharedException;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;

public class GetOpenRouterSettingUseCase(IGetOpenRouterSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<GetOpenRouterSettingQuery, GetOpenRouterSettingResponse>
{
	public async Task<GetOpenRouterSettingResponse> Handle(GetOpenRouterSettingQuery query, CancellationToken ctx)
	{
		var settings = await storage.Get(query.Id, provider.Current.UserId, ctx);
		if (settings is null)
			throw new OpenRouterSettingNotFoundException();

		return new GetOpenRouterSettingResponse
		{
			Model = settings.Model,
			Id = settings.Id
		};
	}
}