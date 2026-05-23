using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class ParseChannelNotFoundException() : NotFoundException("Таких настроек нет!");
