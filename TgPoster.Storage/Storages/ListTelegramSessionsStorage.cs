using Microsoft.EntityFrameworkCore;
using Shared.Telegram;
using TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class ListTelegramSessionsStorage(PosterContext context) : IListTelegramSessionsStorage
{
	public async Task<List<TelegramSessionResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct)
	{
		var items = await context.TelegramSessions
			.Where(s => s.UserId == userId)
			.OrderByDescending(s => s.Created)
			.Select(s => new
			{
				s.Id,
				s.PhoneNumber,
				s.Name,
				s.IsActive,
				s.Status,
				s.Created
			})
			.ToListAsync(ct);

		return items.ConvertAll(s => new TelegramSessionResponse(
			s.Id,
			s.PhoneNumber,
			s.Name,
			s.IsActive,
			(TelegramSessionStatus)s.Status,
			s.Created
		));
	}
}