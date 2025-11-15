using MediatR;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;

public record CreateOpenRouterSettingCommand(string Token, string Model) : IRequest<CreateOpenRouterSettingResponse>;