using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Обнаруженный Telegram-канал/группа.
/// </summary>
public sealed class DiscoveredChannel : BaseEntity
{
	/// <summary>
	///     Username канала (без @).
	/// </summary>
	public required string Username { get; set; }

	/// <summary>
	///     Название канала/группы.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	///     Описание канала.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	///     URL аватарки.
	/// </summary>
	public string? AvatarUrl { get; set; }

	/// <summary>
	///     Количество подписчиков/участников.
	/// </summary>
	public int? ParticipantsCount { get; set; }

	/// <summary>
	///     Тип: "channel" или "chat".
	/// </summary>
	public string? PeerType { get; set; }

	/// <summary>
	///     Уникальный числовой ID канала/чата в Telegram.
	/// </summary>
	public long? TelegramId { get; set; }

	/// <summary>
	///     ID последнего спарсенного сообщения.
	/// </summary>
	public int? LastParsedId { get; set; }

	/// <summary>
	///     Источник обнаружения (tgstat URL или @username исходного канала).
	/// </summary>
	public string? TgUrl { get; set; }

	/// <summary>
	///     Статус обработки.
	/// </summary>
	public DiscoveryStatus Status { get; set; }

	/// <summary>
	///     Основная категория канала (например: "Технологии", "Новости").
	/// </summary>
	public string? Category { get; set; }

	/// <summary>
	///     Подкатегория (например: "AI и ML", "Фондовый рынок").
	/// </summary>
	public string? Subcategory { get; set; }

	/// <summary>
	///     Теги канала.
	/// </summary>
	public string[]? Tags { get; set; }

	/// <summary>
	///     Язык канала (ru, en, uk и т.д.).
	/// </summary>
	public string? Language { get; set; }

	/// <summary>
	///     Уверенность модели в классификации (0.0–1.0).
	/// </summary>
	public double? ClassificationConfidence { get; set; }

	/// <summary>
	///     Дата последней классификации.
	/// </summary>
	public DateTime? ClassifiedAt { get; set; }
}
