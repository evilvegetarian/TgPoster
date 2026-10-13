using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public sealed class ChannelCleanupJobNotFoundException(Guid id)
	: NotFoundException($"Задание на очистку каналов с ID {id} не найдено");
