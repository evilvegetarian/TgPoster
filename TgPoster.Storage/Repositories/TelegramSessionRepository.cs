using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Telegram;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Repositories;

internal sealed class TelegramSessionRepository(PosterContext context)
	: ITelegramSessionRepository, ITelegramAuthRepository
{
	public Task<Guid?> GetByTelegramSessionPurpose(TelegramSessionPurpose purpose, CancellationToken ct) 
		=> context.TelegramSessions
			.Where(s => s.IsActive
			            && s.Purposes.Contains(purpose))
			.Select(s => (Guid?)s.Id)
			.FirstOrDefaultAsync(ct);
	
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

	public async Task DeactivateSessionAsync(Guid sessionId, CancellationToken ct)
	{
		var session = await context.TelegramSessions.FirstAsync(s => s.Id == sessionId, ct);
		session.Status = Data.Enum.TelegramSessionStatus.Failed;
		session.IsActive = false;
		session.SessionData = null;
		await context.SaveChangesAsync(ct);
	}

	public async Task<TelegramSessionDto?> GetByIdAsync(Guid sessionId, CancellationToken ct)
	{
		// Прокси проецируем без кросс-enum каста (Data.Enum.ProxyType со строковым конвертером),
		// иначе (ProxyType)s.Proxy.Type в SQL даёт CAST("Type" AS integer) и падает на строковой колонке
		var session = await context.TelegramSessions
			.Where(s => s.Id == sessionId)
			.Select(s => new
			{
				s.Id,
				s.ApiId,
				s.ApiHash,
				s.PhoneNumber,
				s.IsActive,
				s.UserId,
				s.SessionData,
				Proxy = s.Proxy == null
					? null
					: new
					{
						s.Proxy.Type,
						s.Proxy.Host,
						s.Proxy.Port,
						s.Proxy.Username,
						s.Proxy.Password,
						s.Proxy.Secret
					}
			})
			.FirstOrDefaultAsync(ct);

		if (session is null)
		{
			return null;
		}

		return new TelegramSessionDto
		{
			Id = session.Id,
			ApiId = session.ApiId,
			ApiHash = session.ApiHash,
			PhoneNumber = session.PhoneNumber,
			IsActive = session.IsActive,
			UserId = session.UserId,
			SessionData = session.SessionData,
			Proxy = session.Proxy == null
				? null
				: new ProxyDto(
					(ProxyType)session.Proxy.Type,
					session.Proxy.Host,
					session.Proxy.Port,
					session.Proxy.Username,
					session.Proxy.Password,
					session.Proxy.Secret)
		};
	}
}