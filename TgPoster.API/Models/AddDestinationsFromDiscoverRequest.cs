using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Массовое добавление целевых каналов репоста из Discover
/// </summary>
public sealed class AddDestinationsFromDiscoverRequest
{
	/// <summary>
	///     Id обнаруженных каналов из Discover
	/// </summary>
	[Required(ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MinLength(1, ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MaxLength(20, ErrorMessage = "За один раз можно добавить не больше 20 каналов")]
	public required List<Guid> DiscoveredChannelIds { get; set; }

	/// <summary>
	///     Вступать в канал, если аккаунт ещё не участник. Без вступления репост в канал не работает
	/// </summary>
	[DefaultValue(true)]
	public bool AutoJoin { get; set; } = true;
}
