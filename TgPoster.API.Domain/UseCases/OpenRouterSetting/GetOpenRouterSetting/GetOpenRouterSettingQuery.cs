using MediatR;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;

public record GetOpenRouterSettingQuery(Guid Id) : IRequest<OpenRouterSettingResponse>;