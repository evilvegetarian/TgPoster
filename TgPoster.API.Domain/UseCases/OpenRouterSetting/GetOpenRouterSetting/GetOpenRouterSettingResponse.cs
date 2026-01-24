namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;

public sealed record GetOpenRouterSettingResponse
{
	public required string Model { get; init; }
	public required Guid Id { get; init; }
}