using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

namespace TgPoster.API.Domain.UseCases.PromptSetting.GetPromptSetting;

public class GetPromptSettingUseCase(IGetPromptSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<GetPromptSettingQuery, PromptSettingResponse>
{
	public async Task<PromptSettingResponse> Handle(GetPromptSettingQuery request, CancellationToken cancellationToken)
	{
		var response = await storage.GetAsync(request.Id, provider.Current.UserId, cancellationToken);
		if (response is null)
		{
			throw new PromptSettingNotFoundException(request.Id);
		}

		return response;
	}
}