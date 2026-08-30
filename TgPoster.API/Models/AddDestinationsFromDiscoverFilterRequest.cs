using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Массовое добавление в репост всех каналов Discover, подходящих под фильтр
/// </summary>
public sealed class AddDestinationsFromDiscoverFilterRequest
{
	/// <summary>
	///     Фильтр по тематике (Category)
	/// </summary>
	public string? Category { get; init; }

	/// <summary>
	///     Поиск по названию или username
	/// </summary>
	public string? Search { get; init; }

	/// <summary>
	///     Тип: "channel" (канал) или "chat" (чат). null — без фильтра
	/// </summary>
	public string? PeerType { get; init; }

	/// <summary>
	///     Минимальное число подписчиков (включительно)
	/// </summary>
	[Range(0, int.MaxValue)]
	public int? MinParticipants { get; init; }

	/// <summary>
	///     Максимальное число подписчиков (включительно)
	/// </summary>
	[Range(0, int.MaxValue)]
	public int? MaxParticipants { get; init; }

	/// <summary>
	///     Поле сортировки: задаёт, какие каналы попадут в задание при упирании в лимит
	/// </summary>
	public DiscoverSortBy SortBy { get; init; } = DiscoverSortBy.Participants;

	/// <summary>
	///     Направление сортировки. По умолчанию — по убыванию
	/// </summary>
	public SortDirection SortDirection { get; init; } = SortDirection.Desc;

	/// <summary>
	///     Вступать в канал, если аккаунт ещё не участник. Без вступления репост в канал не работает
	/// </summary>
	[DefaultValue(true)]
	public bool AutoJoin { get; init; } = true;
}
