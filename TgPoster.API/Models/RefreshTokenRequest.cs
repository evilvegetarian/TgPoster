namespace TgPoster.API.Models;

public class RefreshTokenRequest
{
	public required Guid RefreshToken { get; set; }
}