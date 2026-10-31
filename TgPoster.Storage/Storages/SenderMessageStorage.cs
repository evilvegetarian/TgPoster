using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.YouTube;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

namespace TgPoster.Storage.Storages;

internal class SenderMessageStorage(PosterContext context) : ISenderMessageStorage
{
	private const int SendErrorMaxLength = 1000;

	public Task<MessageDetail?> TakeMessageForSendingAsync(Guid messageId, CancellationToken ct)
	{
		return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
		{
			await using var transaction = await context.Database.BeginTransactionAsync(ct);
			var lockKey = $"sender:{messageId:D}";
			await context.Database.ExecuteSqlInterpolatedAsync(
				$"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
			var message = await context.Messages
				.Include(m => m.Schedule).ThenInclude(s => s.TelegramBot)
				.Include(m => m.Schedule).ThenInclude(s => s.YouTubeAccount)
				.Include(m => m.MessageFiles)
				.FirstOrDefaultAsync(m => m.Id == messageId, ct);
			if (message is null)
				return null;

			await context.Entry(message).ReloadAsync(ct);
			if (message.Status is not (MessageStatus.Register or MessageStatus.InHandle))
				return null;

			if (!message.Schedule.IsActive || !message.IsVerified || message.TimePosting > DateTimeOffset.UtcNow)
			{
				message.Status = MessageStatus.Register;
				await context.SaveChangesAsync(ct);
				await transaction.CommitAsync(ct);
				return null;
			}

			message.Status = MessageStatus.Sending;
			await context.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);

			var account = message.Schedule.YouTubeAccount;
			return new MessageDetail
			{
				Api = message.Schedule.TelegramBot.ApiTelegram,
				ChannelId = message.Schedule.ChannelId,
				YouTubeAccount = account is null ? null : new YouTubeAccountDto
				{
					Id = account.Id,
					AccessToken = account.AccessToken,
					RefreshToken = account.RefreshToken,
					ClientId = account.ClientId,
					ClientSecret = account.ClientSecret,
					DefaultTitle = account.DefaultTitle,
					DefaultDescription = account.DefaultDescription,
					DefaultTags = account.DefaultTags,
					AutoPostingVideo = account.AutoPostingVideo
				},
				MessageDto = [new MessageDto
				{
					Id = message.Id,
					ScheduleId = message.ScheduleId,
					Message = message.TextMessage,
					TimePosting = message.TimePosting,
					Signature = message.Schedule.SignatureEnabled ? message.Schedule.SignatureFooter : null,
					File = message.MessageFiles.Where(f => f.ParentFileId is null).OrderBy(f => f.Order)
						.Select(f => new FileDto
						{
							TgFileId = f.TgFileId,
							Caption = f.Caption,
							ContentType = f.ContentType
						}).ToList()
				}]
			};
		});
	}

	public Task<List<MessageDetail>> GetMessagesAsync()
	{
		var time = DateTimeOffset.UtcNow;
		var plusMinute = time.AddMinutes(5);

		return context.Schedules
			.Where(x=>x.IsActive)
			.Where(x => x.Messages.Any(m => m.TimePosting > time
			                                && m.TimePosting <= plusMinute
			                                && m.Status == MessageStatus.Register
			                                && m.IsVerified))
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
					            && m.Status == MessageStatus.Register
					            && m.IsVerified)
					.Select(m => new MessageDto
					{
						Id = m.Id,
						ScheduleId = m.ScheduleId,
						Message = m.TextMessage,
						TimePosting = m.TimePosting,
						Signature = x.SignatureEnabled ? x.SignatureFooter : null,
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
		await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
		{
			await using var transaction = await context.Database.BeginTransactionAsync();
			foreach (var id in ids.Distinct().Order())
			{
				var lockKey = $"sender:{id:D}";
				await context.Database.ExecuteSqlInterpolatedAsync(
					$"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))");
			}
			var messages = await context.Messages.Where(m => ids.Contains(m.Id)).ToListAsync();
			foreach (var message in messages)
			{
				await context.Entry(message).ReloadAsync();
				if (message.Status == MessageStatus.Register)
					message.Status = MessageStatus.InHandle;
			}
			await context.SaveChangesAsync();
			await transaction.CommitAsync();
		});
	}

	public async Task UpdateSendStatusMessageAsync(Guid id)
	{
		var message = await context.Messages
			.Where(m => m.Id == id)
			.FirstOrDefaultAsync();

		if (message != null)
		{
			message.Status = MessageStatus.Send;
			message.SendError = null;
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

	public async Task UpdateErrorStatusMessageAsync(Guid id, string error, CancellationToken ct)
	{
		var message = await context.Messages.FirstOrDefaultAsync(m => m.Id == id, ct);
		if (message != null)
		{
			message.Status = MessageStatus.Error;
			message.SendError = TruncateSendError(error);
			await context.SaveChangesAsync(ct);
		}
	}

	public async Task<List<InterruptedMessage>> RecoverInHandleMessagesAsync(
		DateTimeOffset now,
		string interruptedError,
		CancellationToken ct
	)
	{
		var messages = await context.Messages
			.Where(m => m.Status == MessageStatus.InHandle || m.Status == MessageStatus.Sending)
			.ToListAsync(ct);

		List<InterruptedMessage> interrupted = [];
		foreach (var message in messages)
		{
			if (message.Status == MessageStatus.InHandle && message.TimePosting > now)
			{
				message.Status = MessageStatus.Register;
				continue;
			}

			message.Status = MessageStatus.Error;
			message.SendError = TruncateSendError(interruptedError);
			interrupted.Add(new InterruptedMessage(message.Id, message.ScheduleId));
		}

		await context.SaveChangesAsync(ct);
		return interrupted;
	}

	public async Task SaveTelegramMessageIdsAsync(
		Guid messageId,
		int firstId,
		int[] allIds,
		int? textMessageId,
		CancellationToken ct
	)
	{
		var message = await context.Messages
			.FirstOrDefaultAsync(m => m.Id == messageId, ct);

		if (message != null)
		{
			message.TelegramMessageId = firstId;
			message.TelegramMessageIds = allIds;
			message.TelegramTextMessageId = textMessageId;
			message.Status = MessageStatus.Send;
			message.SendError = null;
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
						.Where(x=>x.ChatStatus ==ChatStatus.Active)
						.Select(d => new RepostDestinationDto
						{
							Id = d.Id,
							ChatIdentifier = d.ChatId
						}).ToList()
				}))
			.ToListAsync(ct);
	}

	private static string TruncateSendError(string error) =>
		error.Length > SendErrorMaxLength ? error[..SendErrorMaxLength] : error;
}
