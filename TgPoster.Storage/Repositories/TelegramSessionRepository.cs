using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Repositories;

internal sealed class TelegramSessionRepository(PosterContext context)
	: ITelegramSessionRepository, ITelegramAuthRepository
{
	public Task<TelegramSessionDto?> GetByIdAsync(Guid sessionId, CancellationToken ct)
	{
		return context.TelegramSessions
			.Where(s => s.Id == sessionId)
			.Select(s => new TelegramSessionDto
			{
				Id = s.Id,
				ApiId = s.ApiId,
				ApiHash = s.ApiHash,
				PhoneNumber = s.PhoneNumber,
				IsActive = s.IsActive,
				UserId = s.UserId,
				SessionData = s.SessionData
			})
			.FirstOrDefaultAsync(ct);
	}

	public async Task UpdateSessionDataAsync(Guid sessionId, string sessionData, CancellationToken ct)
	{
		var session = await context.TelegramSessions.FirstAsync(s => s.Id == sessionId, ct);
		session.SessionData = sessionData;
		await context.SaveChangesAsync(ct);
	}

	public async Task UpdateStatusAsync(Guid sessionId, TelegramSessionStatus status, CancellationToken ct)
	{
		var session = await context.TelegramSessions.FirstAsync(s => s.Id == sessionId, ct);
		session.Status = (Data.Enum.TelegramSessionStatus)status;
		await context.SaveChangesAsync(ct);
	}
}
