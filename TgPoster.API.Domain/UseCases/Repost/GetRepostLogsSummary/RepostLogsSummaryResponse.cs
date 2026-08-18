using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostLogsSummary;

/// <summary>
///     Количество записей журнала с одной и той же причиной.
/// </summary>
public sealed record RepostLogReasonCount
{
	/// <summary>
	///     Причина, по которой репост пропущен или не удался.
	/// </summary>
	public required RepostLogReason Reason { get; init; }

	/// <summary>
	///     Количество таких записей.
	/// </summary>
	public required int Count { get; init; }
}

/// <summary>
///     Сводка по журналу репостов: сколько дошло, сколько пропущено и сколько упало.
/// </summary>
public sealed record RepostLogsSummaryResponse
{
	/// <summary>
	///     Всего записей журнала под фильтр.
	/// </summary>
	public required int Total { get; init; }

	/// <summary>
	///     Успешных репостов.
	/// </summary>
	public required int Success { get; init; }

	/// <summary>
	///     Репостов, завершившихся ошибкой.
	/// </summary>
	public required int Failed { get; init; }

	/// <summary>
	///     Репостов, пропущенных по настройкам рандомизации и лимитам.
	/// </summary>
	public required int Skipped { get; init; }

	/// <summary>
	///     Время последнего успешного репоста.
	/// </summary>
	public DateTime? LastSuccessAt { get; init; }

	/// <summary>
	///     Разбивка неуспешных записей по причинам.
	/// </summary>
	public required List<RepostLogReasonCount> Reasons { get; init; }
}
