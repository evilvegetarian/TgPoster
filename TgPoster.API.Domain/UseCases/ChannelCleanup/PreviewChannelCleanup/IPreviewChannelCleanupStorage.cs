using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.PreviewChannelCleanup;

public interface IPreviewChannelCleanupStorage
{
	/// <summary>
	/// Есть ли у пользователя такая Telegram-сессия
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> SessionExistsAsync(Guid sessionId, Guid userId, CancellationToken ct);

	/// <summary>
	/// Чаты, которые приложение использует через сессию: репост, парсинг, репост комментариев, расписания владельца
	/// </summary>
	/// <param name="sessionId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<UsedTelegramChats> GetUsedChatsAsync(Guid sessionId, CancellationToken ct);
}
