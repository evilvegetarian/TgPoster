using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.RefreshDestinationInfo;

public interface IRefreshDestinationInfoStorage
{
	/// <summary>
	/// Получает TelegramSessionId через цепочку RepostDestination -> RepostSettings, если настройки репоста
	/// принадлежат пользователю
	/// </summary>
	/// <param name="destinationId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid?> GetTelegramSessionIdAsync(Guid destinationId, Guid userId, CancellationToken ct);

	/// <summary>
	///     Получает ChatId для destination.
	/// </summary>
	Task<long?> GetChatIdAsync(Guid destinationId, CancellationToken ct);

	/// <summary>
	///     Обновляет информацию о канале для destination.
	///     Статус членства, переданный как null, не меняется
	/// </summary>
	Task UpdateDestinationInfoAsync(
		Guid destinationId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus? chatStatus,
		string? avatarBase64,
		CancellationToken ct
	);

	/// <summary>
	/// Фиксирует бан аккаунта сессии в чате: автоматика больше не будет пробовать туда пересылать
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task AddAccountBanAsync(Guid telegramSessionId, long chatId, string? error, CancellationToken ct);
}