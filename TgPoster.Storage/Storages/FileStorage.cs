using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class FileStorage(PosterContext context) : IFileStorage
{
	public async Task MarkFileAsUploadedToS3Async(Guid fileId, CancellationToken ct)
	{
		var file = await context.MessageFiles.FirstOrDefaultAsync(f => f.Id == fileId, ct);
		if (file is null)
		{
			return;
		}

		file.IsInS3 = true;
		await context.SaveChangesAsync(ct);
	}
}
