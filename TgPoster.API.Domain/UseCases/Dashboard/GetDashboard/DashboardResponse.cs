using System.ComponentModel.DataAnnotations;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;

/// <summary>
/// Всё для главной страницы: очередь расписаний, ближайшие посты, проблемы за сутки, аккаунты, фоновые задачи
/// </summary>
public sealed record DashboardResponse
{
	/// <summary>
	/// Очередь по каждому расписанию пользователя
	/// </summary>
	[Required]
	public required List<DashboardSchedule> Schedules { get; init; }

	/// <summary>
	/// Ближайшие запланированные посты по всем расписаниям
	/// </summary>
	[Required]
	public required List<DashboardUpcomingPost> UpcomingPosts { get; init; }

	/// <summary>
	/// Последние посты с ошибкой отправки за сутки
	/// </summary>
	[Required]
	public required List<DashboardFailedPost> FailedPosts { get; init; }

	/// <summary>
	/// Сколько всего постов упало за сутки (в списке FailedPosts может быть меньше)
	/// </summary>
	public required int FailedPostsTotal { get; init; }

	/// <summary>
	/// Итоги репостов за сутки
	/// </summary>
	[Required]
	public required DashboardReposts Reposts { get; init; }

	/// <summary>
	/// Telegram-аккаунты пользователя
	/// </summary>
	[Required]
	public required List<DashboardSession> TelegramSessions { get; init; }

	/// <summary>
	/// Источники парсинга, завершившиеся ошибкой
	/// </summary>
	[Required]
	public required List<DashboardParsingProblem> ParsingProblems { get; init; }

	/// <summary>
	/// Фоновые задачи воркера (общие для всех пользователей)
	/// </summary>
	[Required]
	public required List<DashboardWorker> Workers { get; init; }
}

/// <summary>
/// Состояние очереди расписания
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="ChannelName"></param>
/// <param name="IsActive"></param>
/// <param name="HasSlots">Есть хотя бы один день с непустым временем публикации</param>
/// <param name="PlannedCount">Подтверждённых постов в будущем</param>
/// <param name="AwaitingApprovalCount">Постов в будущем, ожидающих подтверждения</param>
/// <param name="NextPostAt">Ближайший подтверждённый пост</param>
/// <param name="LastPlannedAt">Последний подтверждённый пост</param>
/// <param name="QueueDays">На сколько дней вперёд хватит очереди</param>
/// <param name="FailedLast24h">Постов с ошибкой отправки за сутки</param>
public sealed record DashboardSchedule(
	Guid Id,
	string Name,
	string ChannelName,
	bool IsActive,
	bool HasSlots,
	int PlannedCount,
	int AwaitingApprovalCount,
	DateTimeOffset? NextPostAt,
	DateTimeOffset? LastPlannedAt,
	double? QueueDays,
	int FailedLast24h
);

/// <summary>
/// Ближайший запланированный пост
/// </summary>
/// <param name="Id"></param>
/// <param name="ScheduleId"></param>
/// <param name="ScheduleName"></param>
/// <param name="TimePosting"></param>
/// <param name="TextPreview">Первые 120 символов текста</param>
/// <param name="PhotoCount"></param>
/// <param name="VideoCount"></param>
/// <param name="NeedApprove">Пост ещё не подтверждён</param>
public sealed record DashboardUpcomingPost(
	Guid Id,
	Guid ScheduleId,
	string ScheduleName,
	DateTimeOffset TimePosting,
	string? TextPreview,
	int PhotoCount,
	int VideoCount,
	bool NeedApprove
);

/// <summary>
/// Пост, упавший с ошибкой отправки
/// </summary>
/// <param name="Id"></param>
/// <param name="ScheduleId"></param>
/// <param name="ScheduleName"></param>
/// <param name="TimePosting"></param>
/// <param name="SendError"></param>
/// <param name="TextPreview">Первые 120 символов текста</param>
public sealed record DashboardFailedPost(
	Guid Id,
	Guid ScheduleId,
	string ScheduleName,
	DateTimeOffset TimePosting,
	string? SendError,
	string? TextPreview
);

/// <summary>
/// Итоги репостов за сутки
/// </summary>
/// <param name="SuccessCount"></param>
/// <param name="FailedCount"></param>
/// <param name="SkippedCount"></param>
/// <param name="TopErrors">Самые частые ошибки среди неудачных репостов</param>
public sealed record DashboardReposts(
	int SuccessCount,
	int FailedCount,
	int SkippedCount,
	List<DashboardRepostError> TopErrors
);

/// <summary>
/// Ошибка репоста и сколько раз она повторилась
/// </summary>
/// <param name="Error"></param>
/// <param name="Count"></param>
public sealed record DashboardRepostError(string Error, int Count);

/// <summary>
/// Telegram-аккаунт
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="PhoneMasked">Телефон без середины: видны только последние 4 цифры</param>
/// <param name="Status"></param>
/// <param name="IsActive"></param>
/// <param name="FloodWaitUntil"></param>
/// <param name="HasProblem">Аккаунт выключен, не авторизован или ждёт окончания FloodWait</param>
public sealed record DashboardSession(
	Guid Id,
	string? Name,
	string PhoneMasked,
	TelegramSessionStatus Status,
	bool IsActive,
	DateTimeOffset? FloodWaitUntil,
	bool HasProblem
);

/// <summary>
/// Источник парсинга, завершившийся ошибкой
/// </summary>
/// <param name="Id"></param>
/// <param name="Channel"></param>
/// <param name="ScheduleId"></param>
/// <param name="ScheduleName"></param>
/// <param name="LastParseDate"></param>
public sealed record DashboardParsingProblem(
	Guid Id,
	string Channel,
	Guid ScheduleId,
	string ScheduleName,
	DateTime? LastParseDate
);

/// <summary>
/// Фоновая задача воркера
/// </summary>
/// <param name="JobName"></param>
/// <param name="Status"></param>
/// <param name="LastFinishedAt"></param>
/// <param name="NextRunAt"></param>
/// <param name="CooldownUntil"></param>
/// <param name="LastError"></param>
public sealed record DashboardWorker(
	string JobName,
	WorkerJobStateStatus Status,
	DateTimeOffset? LastFinishedAt,
	DateTimeOffset? NextRunAt,
	DateTimeOffset? CooldownUntil,
	string? LastError
);
