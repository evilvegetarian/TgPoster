using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.UseCases.CleanupS3Files;

namespace TgPoster.Storage.Storages.CleanupS3Files;

internal sealed class CleanupS3FilesStorage(PosterContext context) : ICleanupS3FilesStorage
{
	public async Task ResetIsInS3FlagAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken ct)
	{
		var files = await context.MessageFiles
			.Where(f => f.IsInS3 && fileIds.Contains(f.Id))
			.ToListAsync(ct);

		if (files.Count == 0)
		{
			return;
		}

		foreach (var file in files)
		{
			file.IsInS3 = false;
		}

		await context.SaveChangesAsync(ct);
	}
}
