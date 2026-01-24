namespace TgPoster.API.Domain.UseCases.Accounts.RefreshToken;

public sealed record RefreshTokenResponse
{
	public required string AccessToken { get; init; }
	public required Guid RefreshToken { get; init; }
	public required DateTimeOffset RefreshTokenExpiration { get; init; }
}