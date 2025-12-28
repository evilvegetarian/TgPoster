using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.GetMessageById;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal class GetMessageStorage(PosterContext context) : IGetMessageStorage
{
	public Task<MessageDto?> GetMessagesAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Include(x => x.MessageFiles)
			.Include(x => x.Schedule)
			.Where(sch => sch.Schedule.UserId == userId)
			.Where(mess => mess.Id == id)
			.Select(message => new MessageDto
			{
				Id = message.Id,
				TextMessage = message.TextMessage,
				ScheduleId = message.ScheduleId,
				TimePosting = message.TimePosting,
				Files = message.MessageFiles.Select(file => new FileDto
				{
					Id = file.Id,
					ContentType = file.ContentType,
					TgFileId = file.TgFileId,
					Previews = file.Thumbnails.Select(x => new PreviewDto
					{
						Id = x.Id,
						TgFileId = x.TgFileId
					}).ToList()
				}).ToList()
			}).FirstOrDefaultAsync(ct);
	}
}