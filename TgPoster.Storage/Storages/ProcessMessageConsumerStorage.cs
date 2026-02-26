using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
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
				Prompt = ch.Schedule.PromptSetting != null ? ch.Schedule.PromptSetting.TextPrompt : null,
				TelegramSessionId = ch.TelegramSessionId
			})
			.FirstOrDefaultAsync(ct);
	}

	public Task CreateMessageAsync(MessageDto messageDto, CancellationToken ct)
	{
		var id = guidFactory.New();
		var messageFiles = new List<MessageFile>();
		var order = 0;

		foreach (var media in messageDto.Media)
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

			var thumbnails = media.PreviewPhotoIds.Select(x => new MessageFile
			{
				Id = guidFactory.New(),
				MessageId = id,
				TgFileId = x,
				ContentType = "image/jpeg",
				FileType = FileTypes.Thumbnail,
				ParentFileId = messageFileId,
				Order = 0
			}).ToList();

			messageFiles.AddRange(thumbnails);
		}

		var message = new Message
		{
			Id = id,
			TextMessage = messageDto.Text,
			ScheduleId = messageDto.ScheduleId,
			ChannelParsingSettingId = messageDto.ChannelParsingSettingId,
			IsVerified = !messageDto.IsNeedVerified,
			Status = MessageStatus.Register,
			TimePosting = messageDto.TimePosting,
			IsTextMessage = messageDto.Text.IsTextMessage(),
			MessageFiles = messageFiles
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