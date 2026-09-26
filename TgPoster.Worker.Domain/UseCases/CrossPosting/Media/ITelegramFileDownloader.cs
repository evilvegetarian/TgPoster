namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

/// <summary>
///     Загружает файлы из Telegram Bot API
/// </summary>
internal interface ITelegramFileDownloader
{
	/// <summary>
	///     Скачать файл по идентификатору
	/// </summary>
	/// <param name="botToken"></param>
	/// <param name="tgFileId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<byte[]?> DownloadAsync(string botToken, string tgFileId, CancellationToken ct);
}
