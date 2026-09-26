using Microsoft.Extensions.Logging;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

/// <summary>
///     Реализация загрузки медиа для кросс-поста
/// </summary>
internal sealed class CrossPostMediaLoader(ITelegramFileDownloader downloader, ILogger<CrossPostMediaLoader> logger)
	: ICrossPostMediaLoader
{
	public async Task<IReadOnlyList<LoadedImage>> LoadAsync(
		string botToken,
		IReadOnlyList<CrossPostFileDto> files,
		MediaProfile profile,
		CancellationToken ct)
	{
		var result = new List<LoadedImage>();

		foreach (var file in files.OrderBy(x => x.Order))
		{
			if (result.Count >= profile.MaxImages)
			{
				break;
			}

			var sourceFileId = ResolveSourceFileId(file);
			if (sourceFileId is null)
			{
				continue;
			}

			var raw = await downloader.DownloadAsync(botToken, sourceFileId, ct);
			if (raw is null)
			{
				continue;
			}

			var processed = CrossPostImageProcessor.Process(raw, profile);
			if (processed is null)
			{
				logger.LogWarning(
					"Не удалось обработать файл {TgFileId} для кросс-поста",
					sourceFileId);
				continue;
			}

			result.Add(processed);
		}

		return result;
	}

	private static string? ResolveSourceFileId(CrossPostFileDto file)
	{
		if (file.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
		{
			return file.TgFileId;
		}

		if (file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase))
		{
			return file.ThumbnailTgFileId;
		}

		return null;
	}
}
