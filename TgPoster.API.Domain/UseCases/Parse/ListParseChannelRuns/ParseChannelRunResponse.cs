using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;

/// <summary>
/// Один проход источника из журнала парсинга
/// </summary>
public sealed record ParseChannelRunResponse
{
	/// <summary>ID записи журнала</summary>
	public required Guid Id { get; init; }

	/// <summary>ID источника</summary>
	public required Guid ParseChannelId { get; init; }

	/// <summary>Канал-источник</summary>
	public required string Channel { get; init; }

	/// <summary>Расписание, в которое идут посты</summary>
	public required string ScheduleName { get; init; }

	/// <summary>Начало прохода</summary>
	public required DateTimeOffset StartedAt { get; init; }

	/// <summary>Конец прохода</summary>
	public required DateTimeOffset FinishedAt { get; init; }

	/// <summary>Чем закончился проход</summary>
	public required ParseChannelRunStatus Status { get; init; }

	/// <summary>Ошибка, если проход не удался</summary>
	public string? Error { get; init; }

	/// <summary>Сообщений получено из Telegram</summary>
	public required int FetchedMessages { get; init; }

	/// <summary>Постов собрано из сообщений (альбом — один пост)</summary>
	public required int Posts { get; init; }

	/// <summary>Отсеяно стоп-словами</summary>
	public required int SkippedByAvoidWords { get; init; }

	/// <summary>Отсеяно фильтром рекламы</summary>
	public required int SkippedAsAds { get; init; }

	/// <summary>Отсеяно как уже спарсенные</summary>
	public required int SkippedAsDuplicates { get; init; }

	/// <summary>Отсеяно служебных сообщений без текста и медиа</summary>
	public required int SkippedEmpty { get; init; }

	/// <summary>Новых постов отправлено на обработку</summary>
	public required int Published { get; init; }

	/// <summary>Самый свежий ID сообщения канала после прохода</summary>
	public int? LastMessageId { get; init; }
}
