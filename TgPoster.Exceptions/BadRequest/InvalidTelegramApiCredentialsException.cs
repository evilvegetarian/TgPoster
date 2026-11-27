using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Не указаны API-ключи Telegram и не выбрана существующая сессия
/// </summary>
public sealed class InvalidTelegramApiCredentialsException()
	: DomainException("Укажите API ID и API Hash либо выберите существующую сессию с API-ключами");
