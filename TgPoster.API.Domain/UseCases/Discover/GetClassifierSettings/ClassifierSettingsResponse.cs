using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;

/// <summary>
///     Настройки LLM-классификатора каналов вместе со значениями по умолчанию для сброса
/// </summary>
public sealed record ClassifierSettingsResponse
{
	/// <summary>Включён ли классификатор</summary>
	public required bool IsEnabled { get; init; }

	/// <summary>Модель OpenRouter</summary>
	[Required]
	public required string Model { get; init; }

	/// <summary>Сколько каналов классифицировать за один запуск</summary>
	public required int BatchSize { get; init; }

	/// <summary>Как часто запускать классификатор, в минутах</summary>
	public required int IntervalMinutes { get; init; }

	/// <summary>Сколько последних постов канала брать в выборку</summary>
	public required int MessageSampleCount { get; init; }

	/// <summary>Сколько фото из постов отправлять в модель (0 — не отправлять)</summary>
	public required int PhotoCount { get; init; }

	/// <summary>Через сколько дней классифицировать канал заново (null — никогда)</summary>
	public int? ReclassifyAfterDays { get; init; }

	/// <summary>Тематики, из которых модель выбирает ровно одну</summary>
	[Required]
	public required IReadOnlyList<string> Categories { get; init; }

	/// <summary>Системный промпт; плейсхолдер из <see cref="CategoriesPlaceholder" /> заменяется списком тематик</summary>
	[Required]
	public required string SystemPrompt { get; init; }

	/// <summary>Плейсхолдер списка тематик в промпте</summary>
	[Required]
	public required string CategoriesPlaceholder { get; init; }

	/// <summary>Стандартный системный промпт — для кнопки «вернуть как было»</summary>
	[Required]
	public required string DefaultSystemPrompt { get; init; }

	/// <summary>Стандартный список тематик — для кнопки «вернуть как было»</summary>
	[Required]
	public required IReadOnlyList<string> DefaultCategories { get; init; }

	/// <summary>Telegram-сессия классификатора; null — используется сессия с назначением Classification</summary>
	public ClassifierSessionInfo? TelegramSession { get; init; }

	/// <summary>Когда настройки последний раз меняли; null — настройки ещё не сохранялись</summary>
	public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
///     Telegram-сессия, выбранная для классификатора
/// </summary>
public sealed record ClassifierSessionInfo
{
	/// <summary>ID сессии</summary>
	public required Guid Id { get; init; }

	/// <summary>Название сессии</summary>
	public string? Name { get; init; }

	/// <summary>Активна ли сессия: неактивную воркер пропустит</summary>
	public required bool IsActive { get; init; }
}
