namespace TgPoster.API.Domain.UseCases.Accounts.SignIn;

public class SignInResponse
{
    public required Guid RefreshToken { get; set; }
    public required DateTimeOffset RefreshTokenExpiration { get; set; }
    public required string AccessToken { get; set; }
}