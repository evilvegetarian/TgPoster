using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;

/// <summary>
/// Результат обработки одного канала или группы
/// </summary>
public sealed record ChannelCleanupItemResponse
{
	/// <summary>
	/// Telegram-id чата (без префикса -100)
	/// </summary>
	[Required]
	public required long ChatId { get; init; }

	/// <summary>
	/// Название на момент постановки в очередь
	/// </summary>
	[Required]
	public required string Title { get; init; }

	/// <summary>
	/// Username без @
	/// </summary>
	public string? Username { get; init; }

	/// <summary>
	/// Канал или группа
	/// </summary>
	[Required]
	public required ChatType ChatType { get; init; }

	/// <summary>
	/// Итог обработки
	/// </summary>
	[Required]
	public required ChannelCleanupOutcome Outcome { get; init; }

	/// <summary>
	/// Причина, если выйти не удалось
	/// </summary>
	public string? Error { get; init; }
}

/// <summary>
/// Состояние задания на выход из неиспользуемых каналов и групп
/// </summary>
public sealed record ChannelCleanupJobResponse
{
	/// <summary>
	/// Id задания, по нему опрашивается прогресс
	/// </summary>
	[Required]
	public required Guid JobId { get; init; }

	/// <summary>
	/// Текущий статус задания
	/// </summary>
	[Required]
	public required ChannelCleanupStatus Status { get; init; }

	/// <summary>
	/// Всего каналов в задании
	/// </summary>
	[Required]
	public required int TotalCount { get; init; }

	/// <summary>
	/// Из скольких аккаунт вышел (включая те, где он уже не состоял)
	/// </summary>
	[Required]
	public required int LeftCount { get; init; }

	/// <summary>
	/// Сколько оставлено, потому что канал начал использоваться приложением
	/// </summary>
	[Required]
	public required int SkippedCount { get; init; }

	/// <summary>
	/// Сколько не удалось обработать
	/// </summary>
	[Required]
	public required int FailedCount { get; init; }

	/// <summary>
	/// Сколько ещё ждёт обработки
	/// </summary>
	[Required]
	public required int PendingCount { get; init; }

	/// <summary>
	/// Через сколько секунд задание продолжится. Заполнено, когда Telegram ограничил сессию
	/// </summary>
	public int? RetryAfterSeconds { get; init; }

	/// <summary>
	/// Причина паузы или остановки задания
	/// </summary>
	public string? LastError { get; init; }

	/// <summary>
	/// Результат по каждому каналу
	/// </summary>
	[Required]
	public required List<ChannelCleanupItemResponse> Results { get; init; }

	/// <summary>
	/// Собирает ответ и считает итоги по исходам каналов
	/// </summary>
	/// <param name="jobId"></param>
	/// <param name="status"></param>
	/// <param name="retryAfterSeconds"></param>
	/// <param name="lastError"></param>
	/// <param name="results"></param>
	/// <returns></returns>
	internal static ChannelCleanupJobResponse Create(
		Guid jobId,
		ChannelCleanupStatus status,
		int? retryAfterSeconds,
		string? lastError,
		List<ChannelCleanupItemResponse> results
	) => new()
	{
		JobId = jobId,
		Status = status,
		TotalCount = results.Count,
		LeftCount = results.Count(x => x.Outcome is ChannelCleanupOutcome.Left or ChannelCleanupOutcome.NotMember),
		SkippedCount = results.Count(x => x.Outcome == ChannelCleanupOutcome.InUse),
		FailedCount = results.Count(x => x.Outcome is ChannelCleanupOutcome.Failed
			or ChannelCleanupOutcome.NotProcessed),
		PendingCount = results.Count(x => x.Outcome == ChannelCleanupOutcome.Pending),
		RetryAfterSeconds = retryAfterSeconds,
		LastError = lastError,
		Results = results
	};
}
