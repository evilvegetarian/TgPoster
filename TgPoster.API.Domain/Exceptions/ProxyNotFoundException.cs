namespace TgPoster.API.Domain.Exceptions;

public sealed class ProxyNotFoundException(Guid? id = null)
	: NotFoundException($"Прокси с ID {id} не найден.");
