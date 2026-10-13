using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;

/// <summary>
/// Состояние задания на очистку вместе с результатами по каждому каналу
/// </summary>
/// <param name="JobId"></param>
/// <param name="Status"></param>
/// <param name="SessionFloodWaitUntil"></param>
/// <param name="LastError"></param>
/// <param name="Items"></param>
public sealed record ChannelCleanupJobState(
	Guid JobId,
	ChannelCleanupStatus Status,
	DateTimeOffset? SessionFloodWaitUntil,
	string? LastError,
	List<ChannelCleanupItemResponse> Items);

public interface IGetChannelCleanupJobStorage
{
	/// <summary>
	/// Получить состояние задания пользователя
	/// </summary>
	/// <param name="jobId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ChannelCleanupJobState?> GetJobStateAsync(Guid jobId, Guid userId, CancellationToken ct);
}
