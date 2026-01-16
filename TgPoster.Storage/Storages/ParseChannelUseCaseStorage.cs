using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Storage.Storages;

internal class ParseChannelUseCaseStorage(PosterContext context, GuidFactory guidFactory) : IParseChannelUseCaseStorage
{
	public Task<ParseChannelParsingSettings?> GetChannelParsingParametersAsync(Guid id, CancellationToken ct)
	{
		return context.ChannelParsingParameters
			.Where(x => x.Id == id)
			.Select(ch => new ParseChannelParsingSettings
			{
				TelegramBotId = ch.Schedule.TelegramBot.Id,
				AvoidWords = ch.AvoidWords,
				ChannelName = ch.Channel,
				FromDate = ch.DateFrom,
				LastParsedId = ch.LastParseId,
				ToDate = ch.DateTo,
				CheckNewPosts = ch.CheckNewPosts
			})
			.FirstOrDefaultAsync(ct);
	}

	public Task CreateMessagesAsync(List<MessageDto> messages, CancellationToken ct)
	{
		var message = messages.Select(x =>
		{
			var id = guidFactory.New();
			var messageFiles = new List<MessageFile>();
			var order = 0;

			foreach (var media in x.Media)
			{
				var messageFileId = guidFactory.New();
				var mainFile = new MessageFile
				{
					Id = messageFileId,
					ContentType = media.MimeType,
					TgFileId = media.FileId,
					MessageId = id,
					FileType = media.MimeType.GetFileType(),
					ParentFileId = null,
					Order = order++
				};
				messageFiles.Add(mainFile);

				var thumbnails = media.PreviewPhotoIds.Select(thumb => new MessageFile
				{
					Id = guidFactory.New(),
					MessageId = id,
					TgFileId = thumb,
					ContentType = "image/jpeg",
					FileType = FileTypes.Thumbnail,
					ParentFileId = messageFileId,
					Order = 0
				}).ToList();

				messageFiles.AddRange(thumbnails);
			}

			return new Message
			{
				Id = id,
				TextMessage = x.Text,
				ScheduleId = x.ScheduleId,
				IsVerified = !x.IsNeedVerified,
				Status = MessageStatus.Register,
				TimePosting = x.TimePosting,
				IsTextMessage = x.Text.IsTextMessage(),
				MessageFiles = messageFiles
			};
		});

		context.Messages.AddRangeAsync(message, ct);
		return context.SaveChangesAsync(ct);
	}

	public async Task UpdateChannelParsingParametersAsync(
		Guid id,
		int offsetId,
		bool checkNewPosts,
		CancellationToken ct
	)
	{
		var status = checkNewPosts
			? ParsingStatus.Waiting
			: ParsingStatus.Finished;
		var parametrs = await context.ChannelParsingParameters
			.Where(x => x.Id == id)
			.FirstOrDefaultAsync(ct);
		parametrs.Status = status;
		parametrs.LastParseId = offsetId;
		parametrs.LastParseDate = DateTime.UtcNow;
		await context.SaveChangesAsync(ct);
	}

	public async Task UpdateInHandleStatusAsync(Guid id, CancellationToken ct)
	{
		var parametr = await context.ChannelParsingParameters
			.Where(x => x.Id == id)
			.FirstOrDefaultAsync(ct);
		parametr.Status = ParsingStatus.InHandle;
		await context.SaveChangesAsync(ct);
	}

	public async Task UpdateErrorStatusAsync(Guid id, CancellationToken ct)
	{
		var parametr = await context.ChannelParsingParameters
			.Where(x => x.Id == id)
			.FirstOrDefaultAsync(ct);
		parametr.Status = ParsingStatus.Failed;
		await context.SaveChangesAsync(ct);
	}

	public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Days
			.Where(x => x.ScheduleId == scheduleId)
			.ToDictionaryAsync(x => x.DayOfWeek, x => x.TimePostings.OrderBy(time => time).ToList(), ct);
	}

	public Task<List<DateTimeOffset>> GetExistMessageTimePostingAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => x.ScheduleId == scheduleId)
			.Where(x => x.TimePosting > DateTimeOffset.UtcNow)
			.Where(x => x.Status == MessageStatus.Register)
			.Select(x => x.TimePosting)
			.ToListAsync(ct);
	}
}