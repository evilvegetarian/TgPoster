using Security;
using Security.Interfaces;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using TgPoster.Storage;
using TgPoster.Worker.ConfigModels;
using TgPoster.Worker.Domain;
using TgPoster.Worker.Domain.ConfigModels;

var builder = Host.CreateApplicationBuilder(args);

var logger = builder.Configuration.GetSection(nameof(Logger)).Get<Logger>()!;
builder.Services.AddSerilog(x =>
    x.MinimumLevel.Information()
        .Enrich.WithProperty(nameof(logger.Application), logger.Application)
        .WriteTo.GrafanaLoki(
            logger.LogsUrl,
            restrictedToMinimumLevel: LogEventLevel.Verbose,
            propertiesAsLabels: ["Application", "level"]
        )
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
);

var telegramOptions = builder.Configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
builder.Services.AddSingleton(telegramOptions);

builder.Configuration.AddJsonFile("settingTelegram.json", true, true);

builder.Services.AddScoped<ICryptoAES, CryptoAES>();
builder.Services
    .AddDomain(builder.Configuration)
    .AddStorage(builder.Configuration);
var host = builder.Build();
host.AddHangfire();
host.Run();