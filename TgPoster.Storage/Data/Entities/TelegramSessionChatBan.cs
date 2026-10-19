namespace TgPoster.Storage.Data.Entities;

/// <summary>
/// Бан или ограничение конкретного Telegram-аккаунта в конкретном чате. Автоматика не тратит попытки на чат,
/// где этот аккаунт уже получил отказ. Общие запреты чата (писать, отправлять медиа) хранятся в Discover
/// </summary>
public sealed class TelegramSessionChatBan : BaseEntity
{
	/// <summary>
	/// Сессия заблокированного аккаунта
	/// </summary>
	public required Guid TelegramSessionId { get; set; }

	/// <summary>
	/// Id чата в Telegram без префикса -100
	/// </summary>
	public required long ChatId { get; set; }

	/// <summary>
	/// Ошибка Telegram, по которой зафиксирован бан
	/// </summary>
	public string? Error { get; set; }

	/// <summary>
	/// Когда Telegram отказал послений раз
	/// </summary>
	public DateTimeOffset DetectedAt { get; set; }

	/// <summary>
	/// Сессия заблокированного аккаунта
	/// </summary>
	public TelegramSession TelegramSession { get; set; } = null!;
}
