using MediatR;
using Security.Interfaces;
using Shared.SharedException;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.PromptSetting.EditPromptSetting;

public class EditPromptSettingHandler(IEditPromptSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<EditPromptSettingCommand>
{
	public async Task Handle(EditPromptSettingCommand request, CancellationToken cancellationToken)
	{
		if (!await storage.ExistPromptAsync(request.Id, provider.Current.UserId, cancellationToken))
			throw new OpenRouterNotFoundException(request.Id);

		await storage.UpdatePromptAsync(request.Id, request.TextPrompt, request.VideoPrompt, request.PhotoPrompt,
			cancellationToken);
	}
}