using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на подключение аккаунта Bluesky
/// </summary>
public sealed class ConnectBlueskyRequest
{
	/// <summary>
	///     Handle аккаунта Bluesky
	/// </summary>
	[Required]
	[StringLength(253, MinimumLength = 3)]
	public required string Handle { get; set; }

	/// <summary>
	///     App password Bluesky
	/// </summary>
	[Required]
	[StringLength(64, MinimumLength = 8)]
	public required string AppPassword { get; set; }
}
