namespace TgPoster.API.Models;

/// <summary>
///     Запрос на обновление токена доступа
/// </summary>
public class RefreshTokenRequest
{
	/// <summary>
	///     Токен обновления
	/// </summary>
	public required Guid RefreshToken { get; set; }
}