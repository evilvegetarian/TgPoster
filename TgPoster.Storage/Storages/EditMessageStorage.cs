using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.EditMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Mapper;

namespace TgPoster.Storage.Storages;

internal sealed class EditMessageStorage(PosterContext context) : IEditMessageStorage
{
	public Task<bool> ExistMessageAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Where(m => m.Id == messageId)
			.Where(x => x.Schedule.UserId == userId)
			.AnyAsync(ct);
	}

	public async Task UpdateMessageAsync(
		EditMessageCommand messageDto,
		List<MediaFileResult> newMediaFiles,
		CancellationToken ct
	)
	{
		var message = await context.Messages
			.Include(m => m.MessageFiles)
			.FirstOrDefaultAsync(m => m.Id == messageDto.Id, ct);

		message!.TextMessage = messageDto.Text;
		message.TimePosting = messageDto.TimePosting;
		message.ScheduleId = messageDto.ScheduleId;

		var filesToKeep = message.MessageFiles
			.Where(f => messageDto.Files.Contains(f.Id)
			            || (f.ParentFileId.HasValue && messageDto.Files.Contains(f.ParentFileId.Value)))
			.ToList();

		var filesToRemove = message.MessageFiles.Except(filesToKeep).ToList();
		foreach (var file in filesToRemove)
		{
			context.MessageFiles.Remove(file);
		}

		message.MessageFiles = filesToKeep;

		var existingOrder = filesToKeep.Where(x => x.ParentFileId == null).Max(x => (int?)x.Order) ?? -1;
		var newMediaFilesList = newMediaFiles
			.SelectMany((file, index) => file.ToEntity(messageDto.Id, existingOrder + 1 + index)).ToList();

		foreach (var file in newMediaFilesList)
		{
			message.MessageFiles.Add(file);
		}

		await context.SaveChangesAsync(ct);
	}
}