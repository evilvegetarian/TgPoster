using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Массовое включение или выключение целевых каналов репоста
/// </summary>
public sealed class SetRepostDestinationsActiveRequest
{
	/// <summary>
	/// Id целевых каналов
	/// </summary>
	[Required(ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MinLength(1, ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MaxLength(1000, ErrorMessage = "За один раз можно изменить не больше 1000 каналов")]
	public required List<Guid> DestinationIds { get; set; }

	/// <summary>
	/// true — включить каналы, false — выключить
	/// </summary>
	[Required]
	public required bool IsActive { get; set; }
}
