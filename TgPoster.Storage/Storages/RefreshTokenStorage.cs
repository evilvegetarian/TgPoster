using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Accounts.RefreshToken;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal class RefreshTokenStorage(PosterContext context) : IRefreshTokenStorage
{
    public Task<Guid> GetUserIdAsync(Guid refreshToken, CancellationToken ct)
    {
        return context.RefreshSessions
            .Where(x => x.RefreshToken == refreshToken
                        && x.ExpiresAt >= DateTime.UtcNow)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateRefreshSessionAsync(
        Guid refreshTokenOld,
        Guid refreshToken,
        DateTimeOffset refreshDate,
        CancellationToken ct
    )
    {
        var refresh = await context.RefreshSessions.FirstOrDefaultAsync(x=>x.RefreshToken==refreshTokenOld, ct);
        refresh!.RefreshToken = refreshToken;
        refresh.ExpiresAt = refreshDate;
        await context.SaveChangesAsync(ct);
    }
}