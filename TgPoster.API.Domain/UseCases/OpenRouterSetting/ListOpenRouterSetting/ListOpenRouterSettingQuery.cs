using MediatR;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

public record ListOpenRouterSettingQuery : IRequest<ListOpenRouterSettingResponse>;