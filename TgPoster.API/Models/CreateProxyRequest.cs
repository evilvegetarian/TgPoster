using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
/// Запрос на создание прокси
/// </summary>
/// <param name="Name">Имя прокси для отображения</param>
/// <param name="Type">Тип прокси (Socks5/Http/MTProxy)</param>
/// <param name="Host">IP или доменное имя прокси</param>
/// <param name="Port">Порт прокси</param>
/// <param name="Username">Имя пользователя (для SOCKS5/HTTP, опционально)</param>
/// <param name="Password">Пароль (для SOCKS5/HTTP, опционально)</param>
/// <param name="Secret">Секрет для MTProxy (hex/base64)</param>
public sealed record CreateProxyRequest(
	string Name,
	ProxyType Type,
	string Host,
	int Port,
	string? Username,
	string? Password,
	string? Secret
);
