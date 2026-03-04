using Microsoft.AspNetCore.Http;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на импорт Telegram сессии из файла
/// </summary>
public sealed class ImportTelegramSessionRequest
{
	/// <summary>
	///     API ID приложения Telegram
	/// </summary>
	public required string ApiId { get; set; }

	/// <summary>
	///     API Hash приложения Telegram
	/// </summary>
	public required string ApiHash { get; set; }

	/// <summary>
	///     Файл сессии WTelegram (.session)
	/// </summary>
	public required IFormFile SessionFile { get; set; }

	/// <summary>
	///     Название сессии (опционально)
	/// </summary>
	public string? Name { get; set; }
}
