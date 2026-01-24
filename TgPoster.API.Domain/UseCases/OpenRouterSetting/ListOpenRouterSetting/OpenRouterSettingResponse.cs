namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

public sealed record OpenRouterSettingResponse
{
	public required string Model { get; init; }
	public required Guid Id { get; init; }
}