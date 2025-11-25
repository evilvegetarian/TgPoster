using MediatR;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.DeleteOpenRouterSetting;

public record DeleteOpenRouterSettingCommand(Guid Id) : IRequest;
