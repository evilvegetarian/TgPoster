using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class TelegramBotNotPermission()
	: NotFoundException(
		"Телеграмм Боту запрещено отправлять сообщения в канал, добавьте его в канал или измените разрешения.");
