using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;

/// <summary>
/// Сырая очередь расписания: QueueDays считает UseCase
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="ChannelName"></param>
/// <param name="IsActive"></param>
/// <param name="HasSlots"></param>
/// <param name="PlannedCount"></param>
/// <param name="AwaitingApprovalCount"></param>
/// <param name="NextPostAt"></param>
/// <param name="LastPlannedAt"></param>
/// <param name="FailedLast24h"></param>
public sealed record DashboardScheduleRow(
	Guid Id,
	string Name,
	string ChannelName,
	bool IsActive,
	bool HasSlots,
	int PlannedCount,
	int AwaitingApprovalCount,
	DateTimeOffset? NextPostAt,
	DateTimeOffset? LastPlannedAt,
	int FailedLast24h
);

/// <summary>
/// Сырой аккаунт: маскирование телефона и HasProblem считает UseCase
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="PhoneNumber"></param>
/// <param name="Status"></param>
/// <param name="IsActive"></param>
/// <param name="FloodWaitUntil"></param>
public sealed record DashboardSessionRow(
	Guid Id,
	string? Name,
	string PhoneNumber,
	TelegramSessionStatus Status,
	bool IsActive,
	DateTimeOffset? FloodWaitUntil
);

/// <summary>
/// Количество репостов по исходам
/// </summary>
/// <param name="SuccessCount"></param>
/// <param name="FailedCount"></param>
/// <param name="SkippedCount"></param>
public sealed record DashboardRepostCounts(int SuccessCount, int FailedCount, int SkippedCount);

public interface IGetDashboardStorage
{
	/// <summary>
	/// Очередь всех расписаний пользователя
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="now"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DashboardScheduleRow>> GetSchedulesAsync(Guid userId, DateTimeOffset now, CancellationToken ct);

	/// <summary>
	/// Ближайшие будущие посты пользователя, включая неподтверждённые
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="now"></param>
	/// <param name="limit"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DashboardUpcomingPost>> GetUpcomingPostsAsync(
		Guid userId,
		DateTimeOffset now,
		int limit,
		CancellationToken ct
	);

	/// <summary>
	/// Последние упавшие посты пользователя, начиная с указанного момента
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="since"></param>
	/// <param name="limit"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DashboardFailedPost>> GetFailedPostsAsync(
		Guid userId,
		DateTimeOffset since,
		int limit,
		CancellationToken ct
	);

	/// <summary>
	/// Сколько постов пользователя упало, начиная с указанного момента
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="since"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<int> GetFailedPostsCountAsync(Guid userId, DateTimeOffset since, CancellationToken ct);

	/// <summary>
	/// Количество репостов пользователя по исходам, начиная с указанного момента
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="since"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<DashboardRepostCounts> GetRepostCountsAsync(Guid userId, DateTimeOffset since, CancellationToken ct);

	/// <summary>
	/// Самые частые ошибки неудачных репостов пользователя, начиная с указанного момента
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="since"></param>
	/// <param name="limit"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DashboardRepostError>> GetRepostTopErrorsAsync(
		Guid userId,
		DateTimeOffset since,
		int limit,
		CancellationToken ct
	);

	/// <summary>
	/// Telegram-аккаунты пользователя
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DashboardSessionRow>> GetSessionsAsync(Guid userId, CancellationToken ct);

	/// <summary>
	/// Источники парсинга пользователя со статусом ошибки
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DashboardParsingProblem>> GetParsingProblemsAsync(Guid userId, CancellationToken ct);

	/// <summary>
	/// Состояния всех фоновых задач воркера
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DashboardWorker>> GetWorkersAsync(CancellationToken ct);
}
