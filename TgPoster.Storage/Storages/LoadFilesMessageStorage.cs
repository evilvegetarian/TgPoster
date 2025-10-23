using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.LoadFilesMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Mapper;

namespace TgPoster.Storage.Storages;

internal sealed class LoadFilesMessageStorage(PosterContext context, GuidFactory guidFactory) : ILoadFilesMessageStorage
{
	public Task<string?> GetTelegramApiAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Where(m => m.Id == messageId)
			.Where(m => m.Schedule.UserId == userId)
			.Select(m => m.Schedule.TelegramBot.ApiTelegram)
			.FirstOrDefaultAsync(ct);
	}

	public Task<TelegramBotDto?> GetTelegramBotAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Where(m => m.Id == messageId)
			.Where(m => m.Schedule.UserId == userId)
			.Select(m => new TelegramBotDto
			{
				ApiTelegram = m.Schedule.TelegramBot.ApiTelegram,
				ChatId = m.Schedule.TelegramBot.ChatId
			})
			.FirstOrDefaultAsync(ct);
	}

	public async Task AddFileAsync(Guid messageId, List<MediaFileResult> files, CancellationToken ct)
	{
		var messageFiles = files.Select(file => file.ToEntity(messageId));
		await context.MessageFiles.AddRangeAsync(messageFiles, ct);
		await context.SaveChangesAsync(ct);
	}
}