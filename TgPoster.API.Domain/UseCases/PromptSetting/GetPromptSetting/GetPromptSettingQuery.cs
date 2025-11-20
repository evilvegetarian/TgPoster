using MediatR;
using TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

namespace TgPoster.API.Domain.UseCases.PromptSetting.GetPromptSetting;

public record GetPromptSettingQuery(Guid Id) : IRequest<PromptSettingResponse>;