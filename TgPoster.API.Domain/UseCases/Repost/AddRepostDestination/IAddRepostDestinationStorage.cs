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
	///     Создаёт или обновляет запись в Discover для канала и фиксирует статусы прав на отправку.
	///     Сохраняется независимо (до валидации прав), чтобы Discover знал и о недоступных для постинга каналах.
	/// </summary>
	/// <param name="chatId">Id чата/канала в Telegram.</param>
	/// <param name="title">Название канала/чата.</param>
	/// <param name="username">Username канала (без @).</param>
	/// <param name="memberCount">Количество участников/подписчиков.</param>
	/// <param name="chatType">Тип чата (канал/группа).</param>
	/// <param name="canSendMessages">Может ли аккаунт писать в чат.</param>
	/// <param name="canSendMedia">Может ли аккаунт слать медиа в чат.</param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>Id записи Discover.</returns>
	Task<Guid> UpsertDiscoveredChannelAsync(
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		bool canSendMessages,
		bool canSendMedia,
		CancellationToken ct
	);

	/// <summary>
	///     Добавляет новый destination с расширенной информацией о канале, связывая его с записью Discover.
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста.</param>
	/// <param name="chatId">Id чата/канала в Telegram.</param>
	/// <param name="title">Название канала/чата.</param>
	/// <param name="username">Username канала (без @).</param>
	/// <param name="memberCount">Количество участников/подписчиков.</param>
	/// <param name="chatType">Тип чата (канал/группа).</param>
	/// <param name="chatStatus">Статус доступа к чату.</param>
	/// <param name="avatarBase64">Аватарка в формате base64 data URI.</param>
	/// <param name="discoveredChannelId">Id связанной записи Discover.</param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>Id созданного destination.</returns>
	Task<Guid> AddDestinationAsync(
		Guid repostSettingsId,
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus chatStatus,
		string? avatarBase64,
		Guid discoveredChannelId,
		CancellationToken ct
	);
}