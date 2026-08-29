namespace TgPoster.Telegram.Models;

/// <summary>
///     Тип проблемы с Telegram аккаунтом, о которой нужно оповестить владельца
/// </summary>
public enum TelegramSessionProblem
{
	/// <summary>
	///     Авторизация отозвана — требуется повторный вход (SESSION_REVOKED, AUTH_KEY_UNREGISTERED, USER_DEACTIVATED)
	/// </summary>
	AuthorizationRevoked,

	/// <summary>
	///     Ключ авторизации дублирован (AUTH_KEY_DUPLICATED) — сессия используется где-то ещё
	/// </summary>
	AuthKeyDuplicated,

	/// <summary>
	///     Данные сессии повреждены, войти с ними не удалось
	/// </summary>
	SessionCorrupted,

	/// <summary>
	///     Не удалось войти в аккаунт по неизвестной причине
	/// </summary>
	LoginFailed,

	/// <summary>
	///     Telegram ограничил аккаунт по частоте запросов (FLOOD_WAIT)
	/// </summary>
	FloodWait,

	/// <summary>
	///     Аккаунт огрничен за спам (PEER_FLOOD)
	/// </summary>
	SpamRestricted
}
