using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.TelegramSessions.UpdateTelegramSession;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateTelegramSessionStorage(PosterContext context) : IUpdateTelegramSessionStorage
{
	public Task<TelegramSessionDto?> GetByIdAsync(Guid userId, Guid sessionId, CancellationToken ct)
	{
		return context.TelegramSessions
			.Where(s => s.Id == sessionId && s.UserId == userId)
			.Select(s => new TelegramSessionDto
			{
				Id = s.Id,
				Name = s.Name,
				IsActive = s.IsActive
			})
			.FirstOrDefaultAsync(ct);
	}

	public async Task UpdateAsync(Guid sessionId, string? name, bool isActive, CancellationToken ct)
	{
		var session = await context.TelegramSessions
			.FirstAsync(s => s.Id == sessionId, ct);

		session.Name = name;
		session.IsActive = isActive;

		await context.SaveChangesAsync(ct);
	}
}