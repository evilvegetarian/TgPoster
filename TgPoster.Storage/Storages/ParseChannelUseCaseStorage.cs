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
				CheckNewPosts = ch.CheckNewPosts,
			})
			.FirstOrDefaultAsync(ct);
	}

	public Task CreateMessagesAsync(List<MessageDto> messages, CancellationToken ct)
	{
		var message = messages.Select(x =>
		{
			var id = guidFactory.New();
			return new Message
			{
				Id = id,
				TextMessage = x.Text,
				ScheduleId = x.ScheduleId,
				IsVerified = !x.IsNeedVerified,
				Status = MessageStatus.Register,
				TimePosting = x.TimePosting,
				IsTextMessage = x.Text.IsTextMessage(),
				MessageFiles = x.Media.Select<MediaDto, MessageFile>(m =>
				{
					var messageFileId=guidFactory.New();
					var previews = m.PreviewPhotoIds.Select(thumb => new FileThumbnail
					{
						TgFileId = thumb,
						Id = guidFactory.New(),
						MessageFileId = messageFileId,
						ContentType = "image/jpeg",
					}).ToList();

					return new MessageFile
					{
						Id = messageFileId,
						ContentType = m.MimeType,
						TgFileId = m.FileId,
						MessageId = id,
						FileType = m.MimeType.GetFileType(),
						Thumbnails = previews
					};
				}).ToList()
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