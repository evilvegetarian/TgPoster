using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Обновление настроек комментирующего репоста.
/// </summary>
public sealed class UpdateCommentRepostRequest
{
	/// <summary>
	///     Активны ли настройки.
	/// </summary>
	[Required]
	public required bool IsActive { get; set; }
}
