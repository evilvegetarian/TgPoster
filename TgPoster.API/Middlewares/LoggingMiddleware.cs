using Serilog;

namespace TgPoster.API.Middlewares;

/// <summary>
/// </summary>
internal static class LoggingMiddleware
{
    /// <summary>
    ///     Метод добавляет логирование
    /// </summary>
    /// <param name="builder"></param>
    public static void AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
    }
}