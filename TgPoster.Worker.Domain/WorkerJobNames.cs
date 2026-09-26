namespace TgPoster.Worker.Domain;

/// <summary>
///     Имена recurring job'ов Hangfire
/// </summary>
public static class WorkerJobNames
{
	/// <summary>Обнаружение ссылок на каналы.</summary>
	public const string DiscoverChannelLinks = "discover-channel-links-job";

	/// <summary>Еженедельная очистка устаревших файлов в S3.</summary>
	public const string CleanupS3Files = "cleanup-s3-files-job";

	/// <summary>Классификация обнаруженных каналов через LLM</summary>
	public const string ClassifyChannels = "classify-channels-job";
}