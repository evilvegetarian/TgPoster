using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
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

		var files = message.MessageFiles.Where(f => messageDto.Files.Contains(f.Id)).ToList();
		message.MessageFiles = files;
		var newmediafiles = newMediaFiles.Select(file => file.ToEntity(messageDto.Id));
		foreach (var file in newmediafiles)
		{
			message.MessageFiles.Add(file);
		}

		await context.SaveChangesAsync(ct);
	}
}