using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Files;
using TgPoster.API.Domain.UseCases.Files.UploadFileToS3;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class UploadFileToS3Storage(PosterContext context) : IUploadFileToS3Storage
{
	public Task<FileInfoDto?> GetFileInfoAsync(Guid fileId, CancellationToken ct)
	{
		return context.MessageFiles
			.Where(f => f.Id == fileId)
			.Select(f => new FileInfoDto
			{
				TgFileId = f.TgFileId,
				ContentType = f.ContentType,
				IsInS3 = f.IsInS3
			})
			.FirstOrDefaultAsync(ct);
	}

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

	public Task<Guid> GetScheduleIdByFileIdAsync(Guid fileId, CancellationToken ct)
	{
		return context.MessageFiles
			.Where(f => f.Id == fileId)
			.Select(f => f.Message.ScheduleId)
			.FirstAsync(ct);
	}
}
