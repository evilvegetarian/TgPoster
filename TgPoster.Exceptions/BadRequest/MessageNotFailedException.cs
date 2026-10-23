using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Повторить отправку можно только для поста, отправка которого завершилась ошибкой
/// </summary>
/// <param name="id"></param>
public sealed class MessageNotFailedException(Guid id)
	: DomainException($"Сообщение с ID {id} не в статусе ошибки, повторить можно только упавший пост");
