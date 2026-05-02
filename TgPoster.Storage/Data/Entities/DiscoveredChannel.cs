using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Обнаруженный Telegram-канал/группа.
/// </summary>
public sealed class DiscoveredChannel : BaseEntity
{
	/// <summary>
	///     Username канала (без @). Null для приватных каналов.
	/// </summary>
	public string? Username { get; set; }

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
	///     Url канала.
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
	public DateTimeOffset? LastClassifiedAt { get; set; }

	/// <summary>
	///     Хеш инвайт-ссылки (для приватных каналов/чатов).
	/// </summary>
	public string? InviteHash { get; set; }

	/// <summary>
	///     Дата последнего парсинга ссылок канала.
	/// </summary>
	public DateTimeOffset? LastDiscoveredAt { get; set; }

	/// <summary>
	///     Дата последнего обновления статистики подписчиков.
	/// </summary>
	public DateTimeOffset? ParticipantsUpdatedAt { get; set; }

	/// <summary>
	///     ID канала, из которого был обнаружен данный канал (самореферентный FK).
	/// </summary>
	public Guid? DiscoveredFromChannelId { get; set; }

	/// <summary>
	///     Канал-источник обнаружения.
	/// </summary>
	public DiscoveredChannel? DiscoveredFromChannel { get; set; }

	public bool IsBanned { get; set; }
}