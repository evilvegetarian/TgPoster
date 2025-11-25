using MediatR;

namespace TgPoster.API.Domain.UseCases.PromptSetting.EditPromptSetting;

public record EditPromptSettingCommand(Guid Id, string? TextPrompt, string? VideoPrompt, string? PhotoPrompt)
	: IRequest;