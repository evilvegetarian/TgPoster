using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

public interface IAddRepostDestinationStorage
{
	/// <summary>
	///     Получает TelegramSessionId для RepostSettings.
	/// </summary>
	Task<Guid?> GetTelegramSessionIdAsync(Guid repostSettingsId, CancellationToken ct);

	/// <summary>
	///     Проверяет существует ли уже destination с таким ChatIdentifier для данного RepostSettings.
	/// </summary>
	Task<bool> DestinationExistsAsync(Guid repostSettingsId, long chatIdentifier, CancellationToken ct);

	/// <summary>
	///     Добавляет новый destination с расширенной информацией о канале. Возвращает ID созданного destination.
	/// </summary>
	Task<Guid> AddDestinationAsync(
		Guid repostSettingsId,
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus chatStatus,
		string? avatarBase64,
		CancellationToken ct);
}
