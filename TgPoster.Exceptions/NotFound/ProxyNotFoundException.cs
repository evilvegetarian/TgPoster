using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public sealed class ProxyNotFoundException(Guid? id = null)
	: NotFoundException($"Прокси с ID {id} не найден.");