using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public sealed class DiscoveredChannelNotFoundException() : NotFoundException("Площадка не найдена в Discover");
