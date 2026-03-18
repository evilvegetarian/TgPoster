namespace TgPoster.Worker.Domain.ConfigModels;

public class ClassificationOptions
{
	public string? ApiKey { get; set; }
	public string Model { get; set; } = "google/gemini-2.0-flash-001";
}
