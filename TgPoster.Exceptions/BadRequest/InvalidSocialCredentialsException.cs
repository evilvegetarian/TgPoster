using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Неверные учётные данные соцсети
/// </summary>
public sealed class InvalidSocialCredentialsException(string platform)
	: DomainException($"Не удалось войти в {platform}: проверьте логин и пароль приложения");
