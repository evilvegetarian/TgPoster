using Shared.Enums;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.StartChannelCleanup;

/// <summary>
/// Канал, который ставится в очередь на выход
/// </summary>
/// <param name="ChatId"></param>
/// <param name="AccessHash"></param>
/// <param name="IsBasicGroup"></param>
/// <param name="ChatType"></param>
/// <param name="Title"></param>
/// <param name="Username"></param>
public sealed record ChannelCleanupItemDraft(
	long ChatId,
	long AccessHash,
	bool IsBasicGroup,
	ChatType ChatType,
	string Title,
	string? Username);

public interface IStartChannelCleanupStorage
{
	/// <summary>
	/// Есть ли у пользователя такая Telegram-сессия
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> SessionExistsAsync(Guid sessionId, Guid userId, CancellationToken ct);

	/// <summary>
	/// Чаты, которые приложение использует через сессию: репост, парсинг, репост комментариев, расписания владельца
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<UsedTelegramChats> GetUsedChatsAsync(Guid sessionId, CancellationToken ct);

	/// <summary>
	/// Id незавершённого задания на очистку этой сессии
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid?> GetActiveJobIdAsync(Guid sessionId, CancellationToken ct);

	/// <summary>
	/// Создать задание со всеми каналами в статусе Pending
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="items"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid> CreateJobAsync(Guid sessionId, IReadOnlyList<ChannelCleanupItemDraft> items, CancellationToken ct);
}
