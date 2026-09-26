using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Настройки LLM-классификатора каналов
/// </summary>
public sealed class UpdateClassifierSettingsRequest
{
	/// <summary>
	///     Включён ли классификатор
	/// </summary>
	[Required]
	public required bool IsEnabled { get; init; }

	/// <summary>
	///     Модель OpenRouter, например qwen/qwen3-vl-8b-instruct
	/// </summary>
	[Required]
	[StringLength(128, MinimumLength = 1)]
	public required string Model { get; init; }

	/// <summary>
	///     Сколько каналов классифицировать за один запуск
	/// </summary>
	[Required]
	[Range(1, 50)]
	public required int BatchSize { get; init; }

	/// <summary>
	///     Как часто запускать классификатор, в минутах
	/// </summary>
	[Required]
	[Range(1, 1440)]
	public required int IntervalMinutes { get; init; }

	/// <summary>
	///     Сколько последних постов канала брать в выборку
	/// </summary>
	[Required]
	[Range(5, 100)]
	public required int MessageSampleCount { get; init; }

	/// <summary>
	///     Сколько фото из постов отправлять в модель (0 — не отправлять)
	/// </summary>
	[Required]
	[Range(0, 10)]
	public required int PhotoCount { get; init; }

	/// <summary>
	///     Через сколько дней классифицировать канал заново (null — никогда)
	/// </summary>
	[Range(1, 365)]
	public int? ReclassifyAfterDays { get; init; }

	/// <summary>
	///     Тематики, из которых модель выбирает ровно одну
	/// </summary>
	[Required]
	[MinLength(1)]
	[MaxLength(50)]
	public required List<string> Categories { get; init; }

	/// <summary>
	///     Системный промпт; должен содержать {categories}
	/// </summary>
	[Required]
	[StringLength(8000, MinimumLength = 1)]
	public required string SystemPrompt { get; init; }

	/// <summary>
	///     Telegram-сессия классификатора (null — сессия с назначением Classification)
	/// </summary>
	public Guid? TelegramSessionId { get; init; }
}
