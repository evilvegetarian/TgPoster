using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на редактирование сообщения
/// </summary>
public sealed class EditMessageRequest
{
	/// <summary>
	///     Id расписания
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	/// Время поста, не раньше текущего. У опубликованного поста не проверяется и не меняется
	/// </summary>
	public required DateTimeOffset TimePosting { get; set; }

	/// <summary>
	///     Текстовое сообщение
	/// </summary>
	public string? TextMessage { get; set; }

	/// <summary>
	///     Старые файлы сообщения
	/// </summary>
	public List<Guid> OldFiles { get; set; } = [];

	/// <summary>
	///     Новые файлы сообщения
	/// </summary>
	public List<IFormFile> NewFiles { get; set; } = [];

	/// <summary>
	///     Кросс-постить ли пост; null — не менять
	/// </summary>
	public bool? CrossPostEnabled { get; set; }

	/// <summary>
	///     Формат кросс-поста; null — не менять, Inherit — как в настройках расписания
	/// </summary>
	public MessageCrossPostFormat? CrossPostFormat { get; set; }
}
