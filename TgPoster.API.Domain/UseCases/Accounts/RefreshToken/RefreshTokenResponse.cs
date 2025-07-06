namespace TgPoster.API.Domain.UseCases.Accounts.RefreshToken;

public record RefreshTokenResponse
{
    public required string AccessToken { get; set; }
    public Guid RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpiration { get; set; }
}