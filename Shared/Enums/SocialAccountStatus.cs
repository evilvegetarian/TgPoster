namespace Shared.Enums;

/// <summary>
///     Статус подключённого аккаунта соцсети
/// </summary>
public enum SocialAccountStatus
{
	/// <summary>
	///     Активен и готов к публикации
	/// </summary>
	Active = 0,

	/// <summary>
	///     Требуется повторная авторизация
	/// </summary>
	NeedsReauth = 1
}
