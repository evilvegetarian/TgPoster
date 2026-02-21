using Microsoft.AspNetCore.Http;
using Telegram.Bot;

namespace TgPoster.API.Domain.Services;

/// <summary>
///     Интерфейс для работы с файлами через Telegram Bot API.
/// </summary>
public interface ITelegramService
{
	Task<List<MediaFileResult>> GetFileMessageInTelegramByFile(
		TelegramBotClient botClient,
		List<IFormFile> files,
		long chatIdWithBotUser,
		CancellationToken ct);

	Task<byte[]> GetByteFileAsync(TelegramBotClient client, string fileId, CancellationToken ct);
}
