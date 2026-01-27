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
}
