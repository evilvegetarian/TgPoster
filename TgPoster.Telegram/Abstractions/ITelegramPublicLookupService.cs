using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Abstractions;

/// <summary>
///     Сервис определения типа Telegram-сущности по username через публичное HTTP-превью t.me.
///     Не требует MTProto-сессии и подходит для лёгких проверок.
/// </summary>
public interface ITelegramPublicLookupService
{
	/// <summary>
	///     Делает GET-запрос на <c>https://t.me/&lt;username&gt;</c> и определяет тип сущности.
	/// </summary>
	/// <param name="usernameOrUrl">
	///     Username в любом формате: <c>name</c>, <c>@name</c>, <c>t.me/name</c>, <c>https://t.me/name</c>.
	/// </param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>
	///     Результат с <see cref="TelegramPublicEntityInfo" />. Для несуществующего username возвращается
	///     <see cref="TelegramOperationStatus.Success" /> с <see cref="TelegramEntityType.NotFound" />.
	///     При сетевых сбоях — <see cref="TelegramOperationResult{T}.Failed" />.
	/// </returns>
	Task<TelegramOperationResult<TelegramPublicEntityInfo>> LookupAsync(
		string usernameOrUrl,
		CancellationToken ct = default
	);

	/// <summary>
	///     Делает GET-запрос на страницу приглашения <c>https://t.me/+&lt;hash&gt;</c> и извлекает информацию о канале/группе.
	///     Не требует MTProto-сессии — заменяет вызов <c>Messages_CheckChatInvite</c> для лёгких lookup'ов.
	/// </summary>
	/// <param name="inviteHash">Хеш инвайт-ссылки (без префикса <c>+</c> или <c>joinchat/</c>).</param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>
	///     Результат с <see cref="TelegramPublicEntityInfo" />. <see cref="TelegramPublicEntityInfo.Username" /> всегда
	///     <c>null</c>. Для просроченного/некорректного хеша возвращается <see cref="TelegramOperationStatus.Success" />
	///     с <see cref="TelegramEntityType.NotFound" />. При сетевых сбоях — <see cref="TelegramOperationResult{T}.Failed" />.
	/// </returns>
	Task<TelegramOperationResult<TelegramPublicEntityInfo>> LookupInviteAsync(
		string inviteHash,
		CancellationToken ct = default
	);
}