using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.SetRepostDestinationsActive;

public interface ISetRepostDestinationsActiveStorage
{
	/// <summary>
	/// Возвращает Telegram-сессию настроек репоста, если они существуют и принадлежат пользователю
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid?> GetTelegramSessionIdAsync(Guid settingsId, Guid userId, CancellationToken ct);

	/// <summary>
	/// Возвращает те из укзанных каналов, которые относятся к этим настройкам репоста
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="destinationIds"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<RepostDestinationState>> GetDestinationsAsync(
		Guid settingsId,
		List<Guid> destinationIds,
		CancellationToken ct
	);

	/// <summary>
	/// Включает каналы и помечает чат доступным: членство уже проверено
	/// </summary>
	/// <param name="destinationIds"></param>
	/// <param name="ct"></param>
	Task ActivateAsync(List<Guid> destinationIds, CancellationToken ct);

	/// <summary>
	/// Выключает каналы, статус доступа к чату не меняется
	/// </summary>
	/// <param name="destinationIds"></param>
	/// <param name="ct"></param>
	Task DeactivateAsync(List<Guid> destinationIds, CancellationToken ct);
}

/// <summary>
/// Текущее состояние канала: по нему решается, что менять и нужна ли проверка членства
/// </summary>
/// <param name="Id"></param>
/// <param name="ChatId"></param>
/// <param name="IsActive"></param>
/// <param name="ChatStatus"></param>
public sealed record RepostDestinationState(Guid Id, long ChatId, bool IsActive, ChatStatus ChatStatus);
