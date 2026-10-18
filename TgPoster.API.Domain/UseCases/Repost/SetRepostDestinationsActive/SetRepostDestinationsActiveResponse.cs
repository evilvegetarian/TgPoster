namespace TgPoster.API.Domain.UseCases.Repost.SetRepostDestinationsActive;

/// <summary>
/// Итог массового переключения: сколько каналов переключено и сколько пропущено при включении
/// </summary>
public sealed record SetRepostDestinationsActiveResponse
{
	/// <summary>
	/// Каналы, у которых изменился статус
	/// </summary>
	public required int UpdatedCount { get; init; }

	/// <summary>
	/// Не включены: аккаунт не состоит в чате или заблокирован в нём
	/// </summary>
	public required int NotMemberCount { get; init; }

	/// <summary>
	/// Не включены: в чате нельзя отправлять сообщения или медиа
	/// </summary>
	public required int NoPermissionCount { get; init; }
}
