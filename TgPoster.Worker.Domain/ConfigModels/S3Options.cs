namespace TgPoster.Worker.Domain.ConfigModels;

public class S3Options
{
	public required string AccessKey { get; set; }
	public required string SecretKey { get; set; }
	public required string ServiceUrl { get; set; }
	public required string BucketName { get; set; }
}