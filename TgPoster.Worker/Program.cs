using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.AspNetCore.Builder;
using Security;
using Security.Interfaces;
using Serilog;
using TgPoster.Storage;
using TgPoster.Worker.Domain;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var telegramOptions = builder.Configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
builder.Services.AddPrometheusMetrics();
builder.Services.AddSingleton(telegramOptions);
GlobalFFOptions.Configure(options =>
{
	options.LogLevel = FFMpegLogLevel.Debug;
});

builder.Services.AddScoped<ICryptoAES, CryptoAES>();
builder.Services
	.AddDomain(builder.Configuration)
	.AddStorage(builder.Configuration);

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.AddHangfire();

app.Run();
