using MediatR;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;

public record GetOpenRouterSettingQuery(Guid Id) : IRequest<GetOpenRouterSettingResponse>;