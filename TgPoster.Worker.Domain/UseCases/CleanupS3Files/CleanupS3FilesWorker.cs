using Amazon.S3;
using Amazon.S3.Model;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.ConfigModels;

namespace TgPoster.Worker.Domain.UseCases.CleanupS3Files;

/// <summary>
///     Воркер, который раз в неделю удаляет из S3 файлы старше заданного срока
/// </summary>
internal sealed class CleanupS3FilesWorker(
	IAmazonS3 s3,
	S3Options s3Options,
	ICleanupS3FilesStorage storage,
	ILogger<CleanupS3FilesWorker> logger,
	IHostApplicationLifetime lifetime)
{
	/// <summary>Максимальное число ключей в одном запросе на удаление (ограничение S3).</summary>
	private const int DeleteBatchSize = 1000;

	/// <summary>Максимальный возраст файла в S3, после которого он удаляется.</summary>
	private static readonly TimeSpan MaxAge = TimeSpan.FromDays(10);

	[DisableConcurrentExecution(3600)]
	public async Task CleanupAsync()
	{
		var ct = lifetime.ApplicationStopping;
		var threshold = DateTime.UtcNow - MaxAge;

		var keysToDelete = new List<KeyVersion>();
		var fileIdsToReset = new List<Guid>();

		string? continuationToken = null;
		do
		{
			ct.ThrowIfCancellationRequested();

			var listResponse = await s3.ListObjectsV2Async(new ListObjectsV2Request
			{
				BucketName = s3Options.BucketName,
				ContinuationToken = continuationToken
			}, ct);

			foreach (var s3Object in listResponse.S3Objects ?? [])
			{
				if (s3Object.LastModified is null || s3Object.LastModified.Value.ToUniversalTime() >= threshold)
				{
					continue;
				}

				keysToDelete.Add(new KeyVersion { Key = s3Object.Key });
				if (Guid.TryParse(s3Object.Key, out var fileId))
				{
					fileIdsToReset.Add(fileId);
				}
			}

			continuationToken = listResponse.IsTruncated == true ? listResponse.NextContinuationToken : null;
		} while (continuationToken is not null);

		if (keysToDelete.Count == 0)
		{
			logger.LogInformation("В S3 нет файлов старше {Days} дней для удаления", MaxAge.TotalDays);
			return;
		}

		logger.LogInformation("Найдено {Count} файлов старше {Days} дней для удаления из S3",
			keysToDelete.Count, MaxAge.TotalDays);

		var deleted = 0;
		for (var i = 0; i < keysToDelete.Count; i += DeleteBatchSize)
		{
			ct.ThrowIfCancellationRequested();

			var batch = keysToDelete.GetRange(i, Math.Min(DeleteBatchSize, keysToDelete.Count - i));
			var deleteResponse = await s3.DeleteObjectsAsync(new DeleteObjectsRequest
			{
				BucketName = s3Options.BucketName,
				Objects = batch
			}, ct);

			deleted += deleteResponse.DeletedObjects?.Count ?? 0;
		}

		if (fileIdsToReset.Count > 0)
		{
			await storage.ResetIsInS3FlagAsync(fileIdsToReset, ct);
		}

		logger.LogInformation("Удалено {Deleted} файлов из S3", deleted);
	}
}