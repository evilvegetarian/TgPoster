namespace TgPoster.API.Domain.Exceptions;

public class TelegramBotNotPermission()
    : NotFoundException(
        "Телеграмм Боту запрещено отправлять сообщения в канал, добавьте его в канал или измените разрешения.");