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
			.Where(sch => sch.Schedule.UserId == userId)
			.Where(mess => mess.Id == id)
			.Select(message => new MessageDto
			{
				Id = message.Id,
				TextMessage = message.TextMessage,
				ScheduleId = message.ScheduleId,
				TimePosting = message.TimePosting,
				HasYouTubeAccount = message.Schedule.YouTubeAccountId.HasValue,
				Files = message.MessageFiles
					.Where(file => file.ParentFileId == null)
					.OrderBy(file => file.Order)
					.Select(file => new FileDto
					{
						Id = file.Id,
						ContentType = file.ContentType,
						TgFileId = file.TgFileId,
						IsInS3 = file.IsInS3,
						Duration = file.Duration,
						Previews = file.Thumbnails
							.Where(x => x.FileType != Data.Enum.FileTypes.VideoClip)
							.Select(x => new PreviewDto
							{
								Id = x.Id,
								TgFileId = x.TgFileId,
								IsInS3 = x.IsInS3
							}).ToList(),
						VideoClip = file.Thumbnails
							.Where(x => x.FileType == Data.Enum.FileTypes.VideoClip)
							.Select(x => new PreviewDto
							{
								Id = x.Id,
								TgFileId = x.TgFileId,
								IsInS3 = x.IsInS3
							}).FirstOrDefault()
					}).ToList()
			}).FirstOrDefaultAsync(ct);
	}
}