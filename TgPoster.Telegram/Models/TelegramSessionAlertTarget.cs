namespace TgPoster.Telegram.Models;

/// <summary>
///     Куда отправлять оповещение о проблемах с Telegram аккаунтом
/// </summary>
/// <param name="BotToken">Расшифрованный токен бота-оповещателя</param>
/// <param name="ChatId">Чат, в который бот пишет владельцу</param>
/// <param name="SessionName">Название сессии (может отсутствовать)</param>
/// <param name="PhoneNumber">Номер телефона аккаунта</param>
public sealed record TelegramSessionAlertTarget(
	string BotToken,
	long ChatId,
	string? SessionName,
	string PhoneNumber
);
