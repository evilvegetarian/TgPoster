namespace TgPoster.API.Domain.UseCases.Accounts.RefreshToken;

public interface IRefreshTokenStorage
{
    Task<Guid> GetUserIdAsync(Guid refreshToken, CancellationToken ct);
    Task UpdateRefreshSessionAsync(Guid userId, Guid refreshToken, DateTimeOffset refreshDate, CancellationToken ct);
}