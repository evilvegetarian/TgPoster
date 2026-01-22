namespace TgPoster.API.Domain.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда имя Telegram канала пустое или некорректное.
/// </summary>
public sealed class InvalidTelegramChannelException()
	: DomainException("Имя Telegram канала не может быть пустым");
