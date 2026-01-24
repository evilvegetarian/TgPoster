namespace TgPoster.API.Domain.UseCases.Accounts.SignIn;

public sealed record SignInResponse
{
	public required Guid RefreshToken { get; init; }
	public required DateTimeOffset RefreshTokenExpiration { get; init; }
	public required string AccessToken { get; init; }
}