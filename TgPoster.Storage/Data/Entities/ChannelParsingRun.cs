using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
/// Журнал парсинга: один проход источника и что из него получилось
/// </summary>
public sealed class ChannelParsingRun : BaseEntity
{
	/// <summary>
	/// Источник парсинга
	/// </summary>
	public required Guid ChannelParsingSettingId { get; set; }

	/// <summary>
	/// Начало прохода
	/// </summary>
	public required DateTimeOffset StartedAt { get; set; }

	/// <summary>
	/// Конец прохода
	/// </summary>
	public required DateTimeOffset FinishedAt { get; set; }

	/// <summary>
	/// Чем закончился проход
	/// </summary>
	public required ParseChannelRunStatus Status { get; set; }

	/// <summary>
	/// Ошибка, если проход не удался
	/// </summary>
	public string? Error { get; set; }

	/// <summary>
	/// Сколько сообщений получено из Telegram (альбом — несколько сообщений)
	/// </summary>
	public int FetchedMessages { get; set; }

	/// <summary>
	/// Сколько постов собрано из сообщений: альбом считается одним постом
	/// </summary>
	public int Posts { get; set; }

	/// <summary>
	/// Отсеяно стоп-словами
	/// </summary>
	public int SkippedByAvoidWords { get; set; }

	/// <summary>
	/// Отсеяно фильтром рекламы
	/// </summary>
	public int SkippedAsAds { get; set; }

	/// <summary>
	/// Отсеяно как уже спарсенные раньше
	/// </summary>
	public int SkippedAsDuplicates { get; set; }

	/// <summary>
	/// Отсеяно служебных сообщений без текста и медиа
	/// </summary>
	public int SkippedEmpty { get; set; }

	/// <summary>
	/// Сколько новых постов отправлено на обработку
	/// </summary>
	public int Published { get; set; }

	/// <summary>
	/// Самый свежий ID сообщения канала после прохода
	/// </summary>
	public int? LastMessageId { get; set; }

	/// <summary>
	/// Источник парсинга
	/// </summary>
	public ChannelParsingSetting ChannelParsingSetting { get; set; } = null!;
}
