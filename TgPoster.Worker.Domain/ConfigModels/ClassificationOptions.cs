namespace TgPoster.Worker.Domain.ConfigModels;

public class OpenRouterOptions
{
	public string? SecretKey { get; set; }
	public string Model { get; set; } = "x-ai/grok-4-fast";
	public int MessageSampleCount { get; set; } = 25;
}
