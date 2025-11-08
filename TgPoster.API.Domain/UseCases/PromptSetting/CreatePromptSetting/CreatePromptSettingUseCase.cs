using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;

public class CreatePromptSettingUseCase(ICreatePromptSettingStorage storage) : IRequestHandler<CreatePromptSettingCommand>
{
	public async Task Handle(CreatePromptSettingCommand request, CancellationToken ctx)
	{
		if (!await storage.ExistScheduleAsync(request.Schedule, ctx))
			throw new ScheduleNotFoundException(request.Schedule);

		await storage.CreatePromptSettingAsync(request.Schedule, request.TextPrompt, request.VideoPrompt, request.PhotoPrompt, ctx);
	}
}