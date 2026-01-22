namespace TgPoster.API.Configuration;

/// <summary>
///     Политики CORS для настройки разрешенных источников запросов
/// </summary>
/// <param name="AllowedOrigins">Список разрешенных источников (origins)</param>
public record CorsPolicies(List<string> AllowedOrigins);