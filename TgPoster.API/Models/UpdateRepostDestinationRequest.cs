using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Обновление целевого канала репоста.
/// </summary>
public sealed class UpdateRepostDestinationRequest
{
	/// <summary>
	///     Активен ли целевой канал.
	/// </summary>
	[Required]
	public required bool IsActive { get; set; }
}
