using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Telegram сессия пользователя для WTelegram клиента.
/// </summary>
public sealed class TelegramSession : BaseEntity
{
	/// <summary>
	///     API ID из my.telegram.org.
	/// </summary>
	public required string ApiId { get; set; }

	/// <summary>
	///     API Hash из my.telegram.org.
	/// </summary>
	public required string ApiHash { get; set; }

	/// <summary>
	///     Номер телефона Telegram аккаунта.
	/// </summary>
	public required string PhoneNumber { get; set; }

	/// <summary>
	///     Название сессии (для удобства пользователя).
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	///     Активна ли сессия.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>
	///     Статус авторизации сессии.
	/// </summary>
	public TelegramSessionStatus Status { get; set; } = TelegramSessionStatus.AwaitingCode;

	/// <summary>
	///     Данные сессии WTelegram (сериализованные в строку).
	/// </summary>
	public string? SessionData { get; set; }

	/// <summary>
	///     Id владельца сессии.
	/// </summary>
	public required Guid UserId { get; set; }

	/// <summary>
	///     Владелец сессии.
	/// </summary>
	public User User { get; set; } = null!;
}