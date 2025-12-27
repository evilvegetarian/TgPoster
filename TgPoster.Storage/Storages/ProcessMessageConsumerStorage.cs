using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;

namespace TgPoster.Storage.Storages;

internal sealed class ProcessMessageConsumerStorage(PosterContext context, GuidFactory guidFactory)
	: IProcessMessageConsumerStorage
{
	public Task<ParametersDto?> GetChannelParsingParametersAsync(Guid id, CancellationToken ct)
	{
		return context.ChannelParsingParameters
			.Where(x => x.Id == id)
			.Select(ch => new ParametersDto
			{
				Token = ch.Schedule.TelegramBot.ApiTelegram,
				ChatId = ch.Schedule.TelegramBot.ChatId,
				TelegramBotId = ch.Schedule.TelegramBot.Id,
				AvoidWords = ch.AvoidWords,
				ChannelName = ch.Channel,
				DeleteMedia = ch.DeleteMedia,
				DeleteText = ch.DeleteText,
				FromDate = ch.DateFrom,
				LastParsedId = ch.LastParseId,
				ToDate = ch.DateTo,
				IsNeedVerified = ch.NeedVerifiedPosts,
				ScheduleId = ch.ScheduleId,
				CheckNewPosts = ch.CheckNewPosts,
				UseAi = ch.UseAiForPosts,
				TokenOpenRouter =
					ch.Schedule.OpenRouterSetting != null ? ch.Schedule.OpenRouterSetting.TokenHash : null,
				ModelOpenRouter =
					ch.Schedule.OpenRouterSetting != null ? ch.Schedule.OpenRouterSetting.TokenHash : null,
				Prompt = ch.Schedule.PromptSetting != null ? ch.Schedule.PromptSetting.TextPrompt : null
			})
			.FirstOrDefaultAsync(ct);
	}

	public Task CreateMessageAsync(MessageDto messageDto, CancellationToken ct)
	{
		var id = guidFactory.New();
		var message = new Message
		{
			Id = id,
			TextMessage = messageDto.Text,
			ScheduleId = messageDto.ScheduleId,
			IsVerified = !messageDto.IsNeedVerified,
			Status = MessageStatus.Register,
			TimePosting = messageDto.TimePosting,
			IsTextMessage = messageDto.Text.IsTextMessage(),
			MessageFiles = messageDto.Media.Select<MediaDto, MessageFile>(m =>
			{
				var messageFileId = guidFactory.New();

				var thumbnails = m.PreviewPhotoIds.Select(x => new FileThumbnail
				{
					Id = guidFactory.New(),
					MessageFileId = messageFileId,
					TgFileId = x,
					ContentType = "image/jpeg"
				}).ToList();

				return new MessageFile
				{
					Id = guidFactory.New(),
					ContentType = m.MimeType,
					TgFileId = m.FileId,
					MessageId = id,
					Thumbnails = thumbnails,
					FileType = m.MimeType.GetFileType()
				};
			}).ToList()
		};
		context.Messages.AddAsync(message, ct);
		return context.SaveChangesAsync(ct);
	}

	public Task<DateTimeOffset> GeLastMessageTimePostingAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => x.ScheduleId == scheduleId)
			.Where(x => x.TimePosting > DateTimeOffset.UtcNow)
			.Where(x => x.Status != MessageStatus.Cancel || x.Status != MessageStatus.Error)
			.OrderByDescending(x => x.TimePosting)
			.Select(x => x.TimePosting)
			.FirstOrDefaultAsync(ct);
	}

	public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Days
			.Where(x => x.ScheduleId == scheduleId)
			.ToDictionaryAsync(x => x.DayOfWeek, x => x.TimePostings.OrderBy(time => time).ToList(), ct);
	}
}