using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;

public class CreatePromptSettingUseCase(ICreatePromptSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<CreatePromptSettingCommand, CreatePromptSettingResponse>
{
	public async Task<CreatePromptSettingResponse> Handle(CreatePromptSettingCommand request, CancellationToken ctx)
	{
		if (!await storage.ExistScheduleAsync(request.Schedule, provider.Current.UserId, ctx))
		{
			throw new ScheduleNotFoundException(request.Schedule);
		}

		var id = await storage.CreatePromptSettingAsync(request.Schedule, request.TextPrompt, request.VideoPrompt,
			request.PhotoPrompt, ctx);
		return new CreatePromptSettingResponse(id);
	}
}