using Microsoft.EntityFrameworkCore;
using Security.Cryptography;
using TgPoster.Storage.ConfigModels;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Repositories;

internal sealed class TelegramSessionAlertRepository(
	PosterContext context,
	ICryptoAES cryptoAes,
	TelegramSecretOptions options) : ITelegramSessionAlertRepository
{
	public async Task<TelegramSessionAlertTarget?> GetAlertTargetAsync(Guid sessionId, CancellationToken ct)
	{
		var target = await context.TelegramSessions
			.Where(s => s.Id == sessionId && s.NotificationBot != null)
			.Select(s => new
			{
				s.Name,
				s.PhoneNumber,
				EncryptedToken = s.NotificationBot!.ApiTelegram,
				s.NotificationBot!.ChatId
			})
			.FirstOrDefaultAsync(ct);

		if (target is null)
		{
			return null;
		}

		var token = cryptoAes.Decrypt(options.SecretKey, target.EncryptedToken);
		return new TelegramSessionAlertTarget(token, target.ChatId, target.Name, target.PhoneNumber);
	}
}
