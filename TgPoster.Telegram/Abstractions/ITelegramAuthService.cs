using Shared.Enums;

namespace TgPoster.Telegram;

/// <summary>
///     Управление авторизацией Telegram-сессий: запуск, подтверждение кода/пароля, импорт сессий
/// </summary>
public interface ITelegramAuthService
{
	/// <summary>
	///     Удаляет клиента из кеша (при удалении сессии)
	/// </summary>
	/// <param name="sessionId">Идентификатор сессии</param>
	Task RemoveClientAsync(Guid sessionId);

	/// <summary>
	///     Возвращает идентификатор активной авторизованной сессии, помеченной для указанного назначения.
	///     Используется фоновыми воркерами, не привязанными к конкретной сессии пользователя
	/// </summary>
	/// <param name="purpose">Назначение сессии</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>ID сессии или null, если активной сессии для этого назначения нет</returns>
	Task<Guid?> GetSessionIdForPurposeAsync(TelegramSessionPurpose purpose, CancellationToken ct = default);

	/// <summary>
	///     Начинает процесс авторизации: высылает код подтверждения на номер телефона
	/// </summary>
	/// <param name="sessionId">Идентификатор сессии</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Тип следующего ожидаемого шага</returns>
	Task<string> StartAuthAsync(Guid sessionId, CancellationToken ct);

	/// <summary>
	///     Подтверждает код, присланный Telegram'ом на номер
	/// </summary>
	/// <param name="sessionId">Идентификатор сессии</param>
	/// <param name="code">Код подтверждения</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Результат — успех, необходимость 2FA или ошибка</returns>
	Task<VerifyCodeResult> VerifyCodeAsync(Guid sessionId, string code, CancellationToken ct);

	/// <summary>
	///     Отправляет пароль 2FA для завершения авторизации
	/// </summary>
	/// <param name="sessionId">Идентификатор сессии</param>
	/// <param name="password">Пароль</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>true если пароль верен</returns>
	Task<bool> SendPasswordAsync(Guid sessionId, string password, CancellationToken ct);

	/// <summary>
	///     Импортирует существующую сессию из бинарного блоба (Telethon или WTelegram-формат)
	/// </summary>
	/// <param name="apiId">API ID</param>
	/// <param name="apiHash">API Hash</param>
	/// <param name="sessionBytes">Содержимое файла сессии</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Результат импорта (информация о пользователе или ошибка)</returns>
	Task<ImportSessionResult> ImportSessionAsync(string apiId, string apiHash, MemoryStream sessionBytes, CancellationToken ct);
}
