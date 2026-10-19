using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

/// <summary>
/// Запись и снятие бана аккаунта в чате. Репост, комментарии и включение каналов ведут реестр одинаково.
/// Изменения сохраняет вызывающий код, чтобы бан и связанные с ним правки попали в одно сохранение
/// </summary>
internal static class TelegramSessionChatBanExtensions
{
	/// <summary>
	/// Ограничение колонки Error
	/// </summary>
	private const int MaxErrorLength = 500;

	/// <summary>
	/// Фиксирует бан аккаунта в чате. Если бан уже записан, обновляет ошибку и время отказа
	/// </summary>
	/// <param name="context"></param>
	/// <param name="guidFactory"></param>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public static async Task AddOrUpdateChatBanAsync(
		this PosterContext context,
		GuidFactory guidFactory,
		Guid telegramSessionId,
		long chatId,
		string? error,
		CancellationToken ct
	)
	{
		var ban = await context.TelegramSessionChatBans
			.FirstOrDefaultAsync(x => x.TelegramSessionId == telegramSessionId && x.ChatId == chatId, ct);

		if (ban == null)
		{
			ban = new TelegramSessionChatBan
			{
				Id = guidFactory.New(),
				TelegramSessionId = telegramSessionId,
				ChatId = chatId
			};
			await context.TelegramSessionChatBans.AddAsync(ban, ct);
		}

		ban.Error = error is { Length: > MaxErrorLength } ? error[..MaxErrorLength] : error;
		ban.DetectedAt = DateTimeOffset.UtcNow;
	}

	/// <summary>
	/// Снимает бан аккаунта в чате, если он записан
	/// </summary>
	/// <param name="context"></param>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public static async Task RemoveChatBanAsync(
		this PosterContext context,
		Guid telegramSessionId,
		long chatId,
		CancellationToken ct
	)
	{
		var ban = await context.TelegramSessionChatBans
			.FirstOrDefaultAsync(x => x.TelegramSessionId == telegramSessionId && x.ChatId == chatId, ct);

		if (ban != null)
		{
			context.TelegramSessionChatBans.Remove(ban);
		}
	}
}
