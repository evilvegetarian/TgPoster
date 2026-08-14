using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public sealed class RepostImportJobNotFoundException(Guid id)
	: NotFoundException($"Задание на добавление каналов с ID {id} не найдено");
