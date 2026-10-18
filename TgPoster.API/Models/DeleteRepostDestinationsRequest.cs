using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Массовое удаление целевых каналов репоста
/// </summary>
public sealed class DeleteRepostDestinationsRequest
{
	/// <summary>
	/// Id целевых каналов
	/// </summary>
	[Required(ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MinLength(1, ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MaxLength(1000, ErrorMessage = "За один раз можно удалить не больше 1000 каналов")]
	public required List<Guid> DestinationIds { get; set; }
}
