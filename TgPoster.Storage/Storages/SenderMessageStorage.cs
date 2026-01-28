using Microsoft.EntityFrameworkCore;
using Shared.YouTube;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

namespace TgPoster.Storage.Storages;

internal class SenderMessageStorage(PosterContext context) : ISenderMessageStorage
{
	public Task<List<MessageDetail>> GetMessagesAsync()
	{
		var time = DateTimeOffset.UtcNow;
		var plusMinute = time.AddMinutes(5);

		return context.Schedules
			.Where(x => x.Messages.Any(m => m.TimePosting > time
			                                && m.TimePosting <= plusMinute
			                                && m.Status == MessageStatus.Register))
			.Select(x => new MessageDetail
			{
				ChannelId = x.ChannelId,
				Api = x.TelegramBot.ApiTelegram,
				YouTubeAccount = x.YouTubeAccount != null
					? new YouTubeAccountDto
					{
						Id = x.YouTubeAccount.Id,
						AccessToken = x.YouTubeAccount.AccessToken,
						RefreshToken = x.YouTubeAccount.RefreshToken,
						ClientId = x.YouTubeAccount.ClientId,
						ClientSecret = x.YouTubeAccount.ClientSecret,
						DefaultTitle = x.YouTubeAccount.DefaultTitle,
						DefaultDescription = x.YouTubeAccount.DefaultDescription,
						DefaultTags = x.YouTubeAccount.DefaultTags,
						AutoPostingVideo = x.YouTubeAccount.AutoPostingVideo
					}
					: null,
				MessageDto = x.Messages
					.Where(m => m.TimePosting > time
					            && m.TimePosting <= plusMinute
					            && m.Status == MessageStatus.Register)
					.Select(m => new MessageDto
					{
						Id = m.Id,
						Message = m.TextMessage,
						TimePosting = m.TimePosting,
						File = m.MessageFiles
							.Where(f => f.ParentFileId == null)
							.OrderBy(f => f.Order)
							.Select(f => new FileDto
							{
								TgFileId = f.TgFileId,
								Caption = f.Caption,
								ContentType = f.ContentType
							}).ToList()
					}).ToList()
			})
			.ToListAsync();
	}

	public async Task UpdateStatusInHandleMessageAsync(List<Guid> ids)
	{
		var messages = await context.Messages
			.Where(m => ids.Contains(m.Id))
			.ToListAsync();

		foreach (var message in messages)
		{
			message.Status = MessageStatus.InHandle;
		}

		await context.SaveChangesAsync();
	}

	public async Task UpdateSendStatusMessageAsync(Guid id)
	{
		var message = await context.Messages
			.Where(m => m.Id == id)
			.FirstOrDefaultAsync();

		if (message != null)
		{
			message.Status = MessageStatus.Send;
			await context.SaveChangesAsync();
		}
	}

	public async Task UpdateYouTubeTokensAsync(
		Guid youTubeAccountId,
		string accessToken,
		string? refreshToken,
		CancellationToken ct
	)
	{
		var account = await context.YouTubeAccounts
			.Where(x => x.Id == youTubeAccountId)
			.FirstOrDefaultAsync(ct);

		if (account != null)
		{
			account.AccessToken = accessToken;
			account.RefreshToken = refreshToken;
			await context.SaveChangesAsync(ct);
		}
	}

	public async Task SaveTelegramMessageIdAsync(Guid messageId, int telegramMessageId, CancellationToken ct)
	{
		var message = await context.Messages
			.FirstOrDefaultAsync(m => m.Id == messageId, ct);

		if (message != null)
		{
			message.TelegramMessageId = telegramMessageId;
			await context.SaveChangesAsync(ct);
		}
	}

	public Task<List<RepostSettingsDto>> GetRepostSettingsForMessageAsync(Guid messageId, CancellationToken ct)
	{
		return context.Messages
			.Where(m => m.Id == messageId)
			.SelectMany(m => m.Schedule.RepostSettings
				.Where(rs => rs.IsActive)
				.Select(rs => new RepostSettingsDto
				{
					Id = rs.Id,
					ScheduleId = rs.ScheduleId,
					TelegramSessionId = rs.TelegramSessionId,
					Destinations = rs.Destinations
						.Where(d => d.IsActive)
						.Select(d => new RepostDestinationDto
						{
							Id = d.Id,
							ChatIdentifier = d.ChatId
						}).ToList()
				}))
			.ToListAsync(ct);
	}
}