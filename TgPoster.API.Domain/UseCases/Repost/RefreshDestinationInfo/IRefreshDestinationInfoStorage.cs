using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.RefreshDestinationInfo;

public interface IRefreshDestinationInfoStorage
{
	/// <summary>
	///     Получает TelegramSessionId через цепочку RepostDestination -> RepostSettings.
	/// </summary>
	Task<Guid?> GetTelegramSessionIdAsync(Guid destinationId, CancellationToken ct);

	/// <summary>
	///     Получает ChatId для destination.
	/// </summary>
	Task<long?> GetChatIdAsync(Guid destinationId, CancellationToken ct);

	/// <summary>
	///     Обновляет информацию о канале для destination.
	/// </summary>
	Task UpdateDestinationInfoAsync(
		Guid destinationId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus chatStatus,
		string? avatarBase64,
		CancellationToken ct);
}
