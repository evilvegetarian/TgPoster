namespace TgPoster.API.Models;

/// <summary>
///     Обновление настроек репоста.
/// </summary>
public sealed class UpdateRepostSettingsRequest
{
	/// <summary>
	///     Активность настроек репоста.
	/// </summary>
	public required bool IsActive { get; set; }
}
