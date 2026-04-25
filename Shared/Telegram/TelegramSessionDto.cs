using Shared.Enums;

namespace Shared.Telegram;

/// <summary>
///     DTO для Telegram сессии.
/// </summary>
public sealed class TelegramSessionDto
{
	public required Guid Id { get; init; }
	public required string ApiId { get; init; }
	public required string ApiHash { get; init; }
	public required string PhoneNumber { get; init; }
	public required bool IsActive { get; init; }
	public required Guid UserId { get; init; }
	public string? SessionData { get; init; }
	public ProxyDto? Proxy { get; init; }
}

/// <summary>
///     DTO прокси для подключения сессии.
/// </summary>
public sealed record ProxyDto(
	ProxyType Type,
	string Host,
	int Port,
	string? Username,
	string? Password,
	string? Secret);