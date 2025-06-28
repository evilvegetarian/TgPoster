using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using TgPoster.API.ConfigModels;

namespace TgPoster.API.Middlewares;

public static class LoggingMiddleware
{
    public static void AddLogging(this WebApplicationBuilder builder)
    {
        var logger = builder.Configuration.GetSection(nameof(Logger)).Get<Logger>()!;
        var serilog = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.WithProperty(nameof(logger.Application), logger.Application)
            .WriteTo.GrafanaLoki(
                logger.LogsUrl,
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                propertiesAsLabels: ["Application", "level"]
            )
            .WriteTo.Console()
            .WriteTo.File("/app/logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog(serilog);
    }
}