using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.AspNetCore.Builder;
using Security;
using Security.Interfaces;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using TgPoster.Storage;
using TgPoster.Worker.ConfigModels;
using TgPoster.Worker.Domain;
using TgPoster.Worker.Domain.ConfigModels;

var builder = WebApplication.CreateBuilder(args);

var logger = builder.Configuration.GetSection(nameof(Logger)).Get<Logger>()!;
builder.Services.AddSerilog(x =>
    x.MinimumLevel.Information()
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
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
);
var telegramOptions = builder.Configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
builder.Services.AddSingleton(telegramOptions);
GlobalFFOptions.Configure(options => 
{
    options.LogLevel = FFMpegLogLevel.Debug; 
});
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("settingTelegram.json", true, true);
}

builder.Services.AddScoped<ICryptoAES, CryptoAES>();
builder.Services
    .AddDomain(builder.Configuration)
    .AddStorage(builder.Configuration);

var app = builder.Build();
app.AddHangfire();

app.Run();