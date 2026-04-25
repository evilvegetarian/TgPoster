using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Прокси для маршрутизации трафика Telegram сессий (SOCKS5/HTTP/MTProxy).
/// </summary>
public sealed class Proxy : BaseEntity
{
	/// <summary>
	///     Имя прокси для отображения в UI.
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	///     Тип прокси.
	/// </summary>
	public required ProxyType Type { get; set; }

	/// <summary>
	///     Хост (IP/доменное имя) прокси-сервера.
	/// </summary>
	public required string Host { get; set; }

	/// <summary>
	///     Порт прокси-сервера.
	/// </summary>
	public required int Port { get; set; }

	/// <summary>
	///     Имя пользователя для аутентификации (SOCKS5/HTTP).
	/// </summary>
	public string? Username { get; set; }

	/// <summary>
	///     Пароль для аутентификации (SOCKS5/HTTP).
	/// </summary>
	public string? Password { get; set; }

	/// <summary>
	///     Секрет для MTProxy (hex/base64).
	/// </summary>
	public string? Secret { get; set; }

	/// <summary>
	///     Id владельца прокси.
	/// </summary>
	public required Guid UserId { get; set; }

	/// <summary>
	///     Владелец прокси.
	/// </summary>
	public User User { get; set; } = null!;

	/// <summary>
	///     Сессии, использующие этот прокси.
	/// </summary>
	public ICollection<TelegramSession> Sessions { get; set; } = [];
}
