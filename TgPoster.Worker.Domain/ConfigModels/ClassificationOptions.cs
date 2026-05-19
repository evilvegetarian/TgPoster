namespace TgPoster.Worker.Domain.ConfigModels;

public class OpenRouterOptions
{
	public string? SecretKey { get; set; }
	public string Model { get; set; } = "qwen/qwen3-vl-8b-instruct";
	public int MessageSampleCount { get; set; } = 25;
}
