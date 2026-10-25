using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverSettings;

/// <summary>
/// Настройки поиска каналов (Discover)
/// </summary>
public sealed record DiscoverSettingsResponse
{
	/// <summary>Включён ли поиск каналов</summary>
	public required bool IsEnabled { get; init; }

	/// <summary>Как часто запускать поиск каналов, в минутах</summary>
	public required int IntervalMinutes { get; init; }

	/// <summary>Сколько каналов парсит каждая сессия за один запуск</summary>
	public required int ChannelsPerSession { get; init; }

	/// <summary>
	/// Сессии, которые можно отдать поиску каналов: все сессии текущего пользователя
	/// и уже назначенные поиску сессии других пользователей
	/// </summary>
	[Required]
	public required IReadOnlyList<DiscoverSessionOption> Sessions { get; init; }

	/// <summary>Когда настройки последний раз меняли; null — настройки ещё не сохранялись</summary>
	public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Telegram-сессия в списке выбора для поиска каналов
/// </summary>
public sealed record DiscoverSessionOption
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

	/// <summary>Назначена ли сессия поиску каналов</summary>
	public required bool IsSelected { get; init; }

	/// <summary>Принадлежит ли сессия текущему пользователю: чужие можно только видеть</summary>
	public required bool IsOwn { get; init; }
}
