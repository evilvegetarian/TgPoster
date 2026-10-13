using Shared.Enums;
using TgPoster.Telegram.Models;

namespace TgPoster.Worker.Domain.UseCases.CleanupChannels;

/// <summary>
/// Задание на очистку вместе с состоянием сессии
/// </summary>
/// <param name="JobId"></param>
/// <param name="TelegramSessionId"></param>
/// <param name="SessionFloodWaitUntil"></param>
/// <param name="Status"></param>
public sealed record CleanupJobData(
	Guid JobId,
	Guid TelegramSessionId,
	DateTimeOffset? SessionFloodWaitUntil,
	ChannelCleanupStatus Status);

/// <summary>
/// Канал задания, ожидающий выхода
/// </summary>
/// <param name="ItemId"></param>
/// <param name="ChatId"></param>
/// <param name="AccessHash"></param>
/// <param name="IsBasicGroup"></param>
/// <param name="Username"></param>
public sealed record CleanupJobPendingItem(
	Guid ItemId,
	long ChatId,
	long AccessHash,
	bool IsBasicGroup,
	string? Username);

public interface ICleanupChannelsStorage
{
	/// <summary>
	/// Получить задание вместе с состоянием сессии
	/// </summary>
	/// <param name="jobId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<CleanupJobData?> GetJobAsync(Guid jobId, CancellationToken ct);

	/// <summary>
	/// Получить каналы задания, ожидающие выхода, в порядке постановки в очередь
	/// </summary>
	/// <param name="jobId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<CleanupJobPendingItem>> GetPendingItemsAsync(Guid jobId, CancellationToken ct);

	/// <summary>
	/// Чаты, которые приложение использует через сессию: репост, парсинг, репост комментариев, расписания владельца
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<UsedTelegramChats> GetUsedChatsAsync(Guid sessionId, CancellationToken ct);

	/// <summary>
	/// Записать итог обработки канала
	/// </summary>
	/// <param name="itemId"></param>
	/// <param name="outcome"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	Task UpdateItemAsync(Guid itemId, ChannelCleanupOutcome outcome, string? error, CancellationToken ct);

	/// <summary>
	/// Пометить все ещё не обработанные каналы задания как NotProcessed
	/// </summary>
	/// <param name="jobId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	Task MarkPendingItemsNotProcessedAsync(Guid jobId, string error, CancellationToken ct);

	/// <summary>
	/// Обновить статус задания
	/// </summary>
	/// <param name="jobId"></param>
	/// <param name="status"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	Task SetJobStatusAsync(Guid jobId, ChannelCleanupStatus status, string? error, CancellationToken ct);

	/// <summary>
	/// Запомнить, до какого момента Telegram ограничил сессию
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="floodWaitUntil"></param>
	/// <param name="ct"></param>
	Task SetSessionFloodWaitAsync(Guid telegramSessionId, DateTimeOffset floodWaitUntil, CancellationToken ct);
}
