using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Accounts.RefreshToken;
using TgPoster.Storage.Data;

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
		var refresh = await context.RefreshSessions
			.FirstOrDefaultAsync(x => x.RefreshToken == refreshTokenOld, ct);
		if (refresh is null)
			return;

		refresh.PreviousRefreshToken = refreshTokenOld;
		refresh.RefreshToken = refreshToken;
		refresh.ExpiresAt = refreshDate;
		await context.SaveChangesAsync(ct);
	}

	public Task<Guid> GetUserIdByPreviousTokenAsync(Guid previousToken, CancellationToken ct)
	{
		return context.RefreshSessions
			.Where(x => x.PreviousRefreshToken == previousToken)
			.Select(x => x.UserId)
			.FirstOrDefaultAsync(ct);
	}

	public async Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken ct)
	{
		var sessions = await context.RefreshSessions
			.Where(x => x.UserId == userId)
			.ToListAsync(ct);
		context.RefreshSessions.RemoveRange(sessions);
		await context.SaveChangesAsync(ct);
	}
}