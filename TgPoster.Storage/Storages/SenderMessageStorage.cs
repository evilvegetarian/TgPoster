using Microsoft.EntityFrameworkCore;
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
				MessageDto = x.Messages
					.Where(m => m.TimePosting > time
					            && m.TimePosting <= plusMinute
					            && m.Status == MessageStatus.Register)
					.Select(m => new MessageDto
					{
						Id = m.Id,
						Message = m.TextMessage,
						TimePosting = m.TimePosting,
						File = m.MessageFiles.Select(f => new FileDto
						{
							TgFileId = f.TgFileId,
							Caption = f.Caption,
							ContentType = f.ContentType
						}).ToList()
					}).ToList()
			})
			.ToListAsync();
	}

	public Task UpdateErrorStatusMessageAsync(Guid id)
	{
		return context.Messages
			.Where(m => m.Id == id)
			.ExecuteUpdateAsync(m =>
				m.SetProperty(msg => msg.Status, MessageStatus.Error)
			);
	}

	public Task UpdateStatusInHandleMessageAsync(List<Guid> ids)
	{
		return context.Messages
			.Where(m => ids.Contains(m.Id))
			.ExecuteUpdateAsync(m =>
				m.SetProperty(msg => msg.Status, MessageStatus.InHandle)
			);
	}

	public Task UpdateSendStatusMessageAsync(Guid id)
	{
		return context.Messages
			.Where(m => m.Id == id)
			.ExecuteUpdateAsync(m =>
				m.SetProperty(msg => msg.Status, MessageStatus.Send)
			);
	}
}