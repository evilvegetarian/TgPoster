namespace Shared.Telegram;

/// <summary>
///     Статус авторизации Telegram сессии.
/// </summary>
public enum TelegramSessionStatus
{
	/// <summary>
	///     Сессия создана, ожидается отправка кода верификации.
	/// </summary>
	AwaitingCode = 0,

	/// <summary>
	///     Код отправлен, ожидается ввод кода пользователем.
	/// </summary>
	CodeSent = 1,

	/// <summary>
	///     Требуется ввод пароля двухфакторной аутентификации.
	/// </summary>
	AwaitingPassword = 2,

	/// <summary>
	///     Авторизация успешно завершена.
	/// </summary>
	Authorized = 3,

	/// <summary>
	///     Ошибка авторизации.
	/// </summary>
	Failed = 4
}