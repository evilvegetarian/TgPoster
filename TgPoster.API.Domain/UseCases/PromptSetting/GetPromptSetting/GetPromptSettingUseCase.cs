using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions;
using TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;
using TgPoster.Exceptions.NotFound;

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