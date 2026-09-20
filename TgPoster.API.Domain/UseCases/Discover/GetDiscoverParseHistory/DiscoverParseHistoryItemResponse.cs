namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;

/// <summary>
///     Запись истории парсинга: канал, когда он парсился и что это дало
/// </summary>
public sealed record DiscoverParseHistoryItemResponse
{
	/// <summary>ID канала</summary>
	public required Guid Id { get; init; }

	/// <summary>Username канала (без @)</summary>
	public string? Username { get; init; }

	/// <summary>Название канала</summary>
	public string? Title { get; init; }

	/// <summary>URL аватарки</summary>
	public string? AvatarUrl { get; init; }

	/// <summary>Ссылка на канал</summary>
	public string? TgUrl { get; init; }

	/// <summary>Тип: "channel" или "chat"</summary>
	public string? PeerType { get; init; }

	/// <summary>Тематика</summary>
	public string? Category { get; init; }

	/// <summary>Число подписчиков</summary>
	public int? ParticipantsCount { get; init; }

	/// <summary>Статус обработки</summary>
	public required DiscoverChannelStatus Status { get; init; }

	/// <summary>Когда канал парсился последний раз</summary>
	public required DateTimeOffset ParsedAt { get; init; }

	/// <summary>Когда канал был найден</summary>
	public DateTimeOffset? FoundAt { get; init; }

	/// <summary>ID последнего обработанного сообщения: следующий парсинг продолжит с него</summary>
	public int? LastParsedMessageId { get; init; }

	/// <summary>Сколько каналов найдено из этого канала за всё время</summary>
	public required int FoundCount { get; init; }

	/// <summary>Название канала, из которого был найден этот канал</summary>
	public string? SourceTitle { get; init; }

	/// <summary>Username канала, из которого был найден этот канал</summary>
	public string? SourceUsername { get; init; }
}
