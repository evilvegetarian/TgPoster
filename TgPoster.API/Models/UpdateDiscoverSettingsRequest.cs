using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Настройки поиска каналов (Discover)
/// </summary>
public sealed class UpdateDiscoverSettingsRequest
{
	/// <summary>
	/// Включён ли поиск каналов
	/// </summary>
	[Required]
	public required bool IsEnabled { get; init; }

	/// <summary>
	/// Как часто запускать поиск каналов, в минутах
	/// </summary>
	[Required]
	[Range(1, 1440)]
	public required int IntervalMinutes { get; init; }

	/// <summary>
	/// Сколько каналов парсит каждая сессия за один запуск
	/// </summary>
	[Required]
	[Range(1, 20)]
	public required int ChannelsPerSession { get; init; }

	/// <summary>
	/// Свои Telegram-сессии, которыми парсятся каналы; с остальных своих сессий назначение снимается,
	/// чужие не меняются
	/// </summary>
	[Required]
	[MaxLength(50)]
	public required List<Guid> TelegramSessionIds { get; init; }
}
