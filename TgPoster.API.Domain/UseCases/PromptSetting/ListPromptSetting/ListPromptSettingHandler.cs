using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

public class ListPromptSettingHandler(IListPromptSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<ListPromptSettingQuery, PagedResponse<PromptSettingResponse>>
{
	public async Task<PagedResponse<PromptSettingResponse>> Handle(
		ListPromptSettingQuery request,
		CancellationToken cancellationToken
	)
	{
		var response = await storage.GetAsync(provider.Current.UserId, cancellationToken);
		return new PagedResponse<PromptSettingResponse>(response, response.Count, request.PageNumber, request.PageSize);
	}
}