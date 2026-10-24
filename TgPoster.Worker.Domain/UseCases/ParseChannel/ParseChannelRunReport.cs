using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

/// <summary>
/// Итог одного прохода источника для журнала парсинга
/// </summary>
public sealed record ParseChannelRunReport
{
	public required Guid ChannelParsingSettingId { get; init; }
	public required DateTimeOffset StartedAt { get; init; }
	public required DateTimeOffset FinishedAt { get; init; }
	public required ParseChannelRunStatus Status { get; init; }
	public string? Error { get; init; }
	public int FetchedMessages { get; init; }
	public int Posts { get; init; }
	public int SkippedByAvoidWords { get; init; }
	public int SkippedAsAds { get; init; }
	public int SkippedAsDuplicates { get; init; }
	public int SkippedEmpty { get; init; }
	public int Published { get; init; }
	public int? LastMessageId { get; init; }

	/// <summary>
	/// Проход, который упал исключением: до счётчиков дело не дошло
	/// </summary>
	/// <param name="channelParsingSettingId"></param>
	/// <param name="startedAt"></param>
	/// <param name="finishedAt"></param>
	/// <param name="error"></param>
	/// <returns></returns>
	public static ParseChannelRunReport Failed(
		Guid channelParsingSettingId,
		DateTimeOffset startedAt,
		DateTimeOffset finishedAt,
		string error
	) => new()
	{
		ChannelParsingSettingId = channelParsingSettingId,
		StartedAt = startedAt,
		FinishedAt = finishedAt,
		Status = ParseChannelRunStatus.Failed,
		Error = error
	};
}
