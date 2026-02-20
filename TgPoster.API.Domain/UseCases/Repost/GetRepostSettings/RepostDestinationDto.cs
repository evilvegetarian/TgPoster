using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

/// <summary>
///     Информация о канале для репоста.
/// </summary>
public sealed record RepostDestinationDto
{
	/// <summary>
	///     Id назначения репоста.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Id чата в Telegram.
	/// </summary>
	public required long ChatId { get; init; }

	/// <summary>
	///     Активность канала для репоста.
	/// </summary>
	public required bool IsActive { get; init; }

	/// <summary>
	///     Название канала/чата.
	/// </summary>
	public string? Title { get; init; }

	/// <summary>
	///     Username (без @).
	/// </summary>
	public string? Username { get; init; }

	/// <summary>
	///     Количество подписчиков.
	/// </summary>
	public int? MemberCount { get; init; }

	/// <summary>
	///     Тип чата.
	/// </summary>
	public ChatType ChatType { get; init; }

	/// <summary>
	///     Статус доступа к чату.
	/// </summary>
	public ChatStatus ChatStatus { get; init; }

	/// <summary>
	///     Аватарка в формате base64 data URI.
	/// </summary>
	public string? AvatarBase64 { get; init; }

	/// <summary>
	///     Дата последнего обновления информации.
	/// </summary>
	public DateTimeOffset? InfoUpdatedAt { get; init; }
}
