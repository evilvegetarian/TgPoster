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

	/// <summary>
	///     Сессии, которые можно отдать классификатору: все сессии текущего пользователя
	///     и уже назначенные классификатору сессии других пользователей
	/// </summary>
	[Required]
	public required IReadOnlyList<ClassifierSessionOption> Sessions { get; init; }

	/// <summary>Когда настройки последний раз меняли; null — настройки ещё не сохранялись</summary>
	public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
///     Telegram-сессия в списке выбора для классификатора
/// </summary>
public sealed record ClassifierSessionOption
{
	/// <summary>ID сессии</summary>
	public required Guid Id { get; init; }

	/// <summary>Название сессии</summary>
	public string? Name { get; init; }

	/// <summary>Номер телефона — только для своих сессий</summary>
	public string? PhoneNumber { get; init; }

	/// <summary>Активна ли сессия: неактивную воркер пропустит</summary>
	public required bool IsActive { get; init; }

	/// <summary>Авторизована ли сессия в Telegram</summary>
	public required bool IsAuthorized { get; init; }

	/// <summary>Назначена ли сессия классификатору</summary>
	public required bool IsSelected { get; init; }

	/// <summary>Принадлежит ли сессия текущему пользователю: чужие можно только видеть</summary>
	public required bool IsOwn { get; init; }
}
