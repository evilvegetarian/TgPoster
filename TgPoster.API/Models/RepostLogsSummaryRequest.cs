namespace TgPoster.API.Models;

/// <summary>
///     Запрос сводки по журналу репостов
/// </summary>
public sealed class RepostLogsSummaryRequest
{
	/// <summary>
	///     Считать только по этим настройкам репоста
	/// </summary>
	public Guid? RepostSettingsId { get; init; }

	/// <summary>
	///     Считать только по конкретному целевому каналу
	/// </summary>
	public Guid? DestinationId { get; init; }

	/// <summary>
	///     Считать только по конкретному сообщению
	/// </summary>
	public Guid? MessageId { get; init; }

	/// <summary>
	///     Начало периода (включительно)
	/// </summary>
	public DateTimeOffset? From { get; init; }

	/// <summary>
	///     Конец периода (включительно)
	/// </summary>
	public DateTimeOffset? To { get; init; }
}
