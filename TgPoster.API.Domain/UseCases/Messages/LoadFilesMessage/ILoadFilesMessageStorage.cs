using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;

namespace TgPoster.API.Domain.UseCases.Messages.LoadFilesMessage;

public interface ILoadFilesMessageStorage
{
	Task<string?> GetTelegramApiAsync(Guid messageId, Guid userId, CancellationToken ct);

	Task<TelegramBotDto?> GetTelegramBotAsync(Guid scheduleId, Guid userId, CancellationToken ct);

	//Task<List<FileModel>> AddFilesAsync(List<IFormFile> files, CancellationToken ct);
	Task AddFileAsync(Guid messageId, List<MediaFileResult> files, CancellationToken ct);
}