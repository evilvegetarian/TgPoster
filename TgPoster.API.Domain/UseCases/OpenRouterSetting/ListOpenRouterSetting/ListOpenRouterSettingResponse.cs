namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

public sealed record ListOpenRouterSettingResponse
{
	public required List<OpenRouterSettingResponse> OpenRouterSettingResponses { get; init; } = [];
}