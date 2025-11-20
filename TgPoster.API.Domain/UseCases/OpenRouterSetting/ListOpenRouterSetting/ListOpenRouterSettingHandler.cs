using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

public class ListOpenRouterSettingHandler(IListOpenRouterSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<ListOpenRouterSettingQuery, ListOpenRouterSettingResponse>
{
	public async Task<ListOpenRouterSettingResponse> Handle(
		ListOpenRouterSettingQuery request,
		CancellationToken cancellationToken
	)
	{
		var response = await storage.GetAsync(provider.Current.UserId, cancellationToken);
		return new ListOpenRouterSettingResponse
		{
			OpenRouterSettingResponses = response.Select(x => new OpenRouterSettingResponse
			{
				Id = x.Id,
				Model = x.Model,
			}).ToList()
		};
	}
}