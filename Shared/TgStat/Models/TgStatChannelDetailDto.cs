namespace Shared.TgStat.Models;

/// <summary>
///     Данные канала со страницы TGStat.
/// </summary>
public sealed class TgStatChannelDetailDto
{
	/// <summary>
	///     URL канала на tgstat.
	/// </summary>
	public required string TgUrl { get; set; }

	/// <summary>
	///     Username канала (без @).
	/// </summary>
	public string? Username { get; set; }

	/// <summary>
	///     Название канала.
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Описание канала.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	///     URL аватарки.
	/// </summary>
	public string? AvatarUrl { get; set; }

	/// <summary>
	///     Количество подписчиков.
	/// </summary>
	public int ParticipantsCount { get; set; }

	/// <summary>
	///     Тип: "channel" или "chat".
	/// </summary>
	public string? PeerType { get; set; }
}
