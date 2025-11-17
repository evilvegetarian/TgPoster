using MediatR;

namespace TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;

public record CreatePromptSettingCommand(Guid Schedule, string? TextPrompt, string? VideoPrompt, string? PhotoPrompt)
	: IRequest<CreatePromptSettingResponse>;