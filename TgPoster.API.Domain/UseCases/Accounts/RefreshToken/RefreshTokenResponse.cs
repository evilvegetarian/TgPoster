namespace TgPoster.API.Domain.UseCases.Accounts.RefreshToken;

public record RefreshTokenResponse
{
	public required string AccessToken { get; set; }
	public required Guid RefreshToken { get; set; }
	public required DateTimeOffset RefreshTokenExpiration { get; set; }
}