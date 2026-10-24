using Shared.Enums;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

namespace TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;

/// <summary>
/// Статистика парсинга: источники, созданные посты, журнал проходов и таймлайны
/// </summary>
public sealed record ParseChannelStatsResponse
{
	/// <summary>Счётчики по источникам</summary>
	public required ParseChannelSourceTotals Sources { get; init; }

	/// <summary>Посты, созданные парсингом, по состоянию</summary>
	public required ParseChannelPostTotals Posts { get; init; }

	/// <summary>Проходы за последние 24 часа</summary>
	public required ParseChannelRunTotals Last24Hours { get; init; }

	/// <summary>Проходы за последние 7 дней</summary>
	public required ParseChannelRunTotals Last7Days { get; init; }

	/// <summary>Проходы за последние 30 дней</summary>
	public required ParseChannelRunTotals Last30Days { get; init; }

	/// <summary>Проходы за выбранный период</summary>
	public required ParseChannelRunTotals Period { get; init; }

	/// <summary>Когда закончился последний проход любого источника</summary>
	public DateTimeOffset? LastRunAt { get; init; }

	/// <summary>Количество источников по каждому статусу (все статусы, включая нулевые)</summary>
	public required List<ParseChannelSourceStatusCount> SourcesByStatus { get; init; }

	/// <summary>Сколько новых постов отправлено на обработку в каждый день окна</summary>
	public required List<DiscoverDailyCount> PublishedByDay { get; init; }

	/// <summary>Сколько постов отсеяно в каждый день окна</summary>
	public required List<DiscoverDailyCount> SkippedByDay { get; init; }

	/// <summary>Источники с наибольшим числом новых постов за период</summary>
	public required List<ParseChannelSourceStat> TopSources { get; init; }
}

/// <summary>
/// Счётчики по источникам парсинга
/// </summary>
public sealed record ParseChannelSourceTotals
{
	/// <summary>Всего источников</summary>
	public required int Total { get; init; }

	/// <summary>Источников, которые ещё парсятся: не закончены и не отменены</summary>
	public required int Active { get; init; }

	/// <summary>Источников в статусе ошибки</summary>
	public required int Failed { get; init; }
}

/// <summary>
/// Посты, созданные парсингом и ещё не удалённые
/// </summary>
public sealed record ParseChannelPostTotals
{
	/// <summary>Всего постов</summary>
	public required int Total { get; init; }

	/// <summary>Подтверждены и ждут публикации</summary>
	public required int Planned { get; init; }

	/// <summary>Ждут подтверждения</summary>
	public required int AwaitingApproval { get; init; }

	/// <summary>Опубликованы</summary>
	public required int Published { get; init; }

	/// <summary>Не отправились из-за ошибки</summary>
	public required int Failed { get; init; }

	/// <summary>Отменены</summary>
	public required int Canceled { get; init; }
}

/// <summary>
/// Суммы по журналу проходов за период
/// </summary>
public sealed record ParseChannelRunTotals
{
	/// <summary>Пустые суммы: за период не было ни одного прохода</summary>
	public static ParseChannelRunTotals Empty { get; } = new()
	{
		Runs = 0,
		FailedRuns = 0,
		FetchedMessages = 0,
		Posts = 0,
		SkippedByAvoidWords = 0,
		SkippedAsAds = 0,
		SkippedAsDuplicates = 0,
		SkippedEmpty = 0,
		Published = 0
	};

	/// <summary>Проходов</summary>
	public required int Runs { get; init; }

	/// <summary>Проходов, закончившихся не успехом</summary>
	public required int FailedRuns { get; init; }

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
}

/// <summary>
/// Статус источника парсинга, значения совпадают с хранимыми
/// </summary>
public enum ParseChannelSourceStatus
{
	/// <summary>Только что создан</summary>
	New = 0,

	/// <summary>Парсится сейчас</summary>
	InHandle = 1,

	/// <summary>Отменён пользователем</summary>
	Canceled = 2,

	/// <summary>Ждёт следующего прохода</summary>
	Waiting = 3,

	/// <summary>Разовый источник спарсен до конца</summary>
	Finished = 4,

	/// <summary>Ошибка</summary>
	Failed = 100
}

/// <summary>
/// Количество источников с одним статусом
/// </summary>
public sealed record ParseChannelSourceStatusCount
{
	/// <summary>Статус</summary>
	public required ParseChannelSourceStatus Status { get; init; }

	/// <summary>Количество источников</summary>
	public required int Count { get; init; }
}

/// <summary>
/// Источник парсинга и его проходы за период
/// </summary>
public sealed record ParseChannelSourceStat
{
	/// <summary>ID источника</summary>
	public required Guid Id { get; init; }

	/// <summary>Канал-источник</summary>
	public required string Channel { get; init; }

	/// <summary>Расписание, в которое идут посты</summary>
	public required string ScheduleName { get; init; }

	/// <summary>Текущий статус источника</summary>
	public required ParseChannelSourceStatus Status { get; init; }

	/// <summary>Проходов за период</summary>
	public required int Runs { get; init; }

	/// <summary>Неудачных проходов за период</summary>
	public required int FailedRuns { get; init; }

	/// <summary>Новых постов за период</summary>
	public required int Published { get; init; }

	/// <summary>Отсеяно постов за период</summary>
	public required int Skipped { get; init; }

	/// <summary>Когда закончился последний проход</summary>
	public DateTimeOffset? LastRunAt { get; init; }

	/// <summary>Чем закончился последний проход</summary>
	public ParseChannelRunStatus? LastRunStatus { get; init; }

	/// <summary>Неудалённых постов из этого источника</summary>
	public required int ParsedMessagesCount { get; init; }

	/// <summary>Сообщений в канале по последним данным из Telegram</summary>
	public int? TotalMessagesCount { get; init; }
}
