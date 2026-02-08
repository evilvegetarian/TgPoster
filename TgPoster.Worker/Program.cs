using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.AspNetCore.Builder;
using Security.Cryptography;
using Security.IdentityServices;
using Serilog;
using Shared;
using TgPoster.Storage;
using TgPoster.Worker.Configuration;
using TgPoster.Worker.Domain;
using TgPoster.Worker.Telemetry;
using TelegramOptions = TgPoster.Worker.Domain.ConfigModels.TelegramOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

var telegramOptions = builder.Configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
builder.Services.AddSingleton(telegramOptions);

var tracingConfiguration = builder.Configuration.GetSection(nameof(TracingConfiguration)).Get<TracingConfiguration>()!;
builder.Services.AddMonitors(tracingConfiguration);

GlobalFFOptions.Configure(options =>
{
	options.LogLevel = FFMpegLogLevel.Debug;
});
builder.Services.AddScoped<ICryptoAES, CryptoAES>();
builder.Services.AddScoped<IIdentityProvider, DesignTimeIdentityProvider>();
builder.Services
	.AddShared()
	.AddDomain(builder.Configuration)
	.AddStorage(builder.Configuration);

var connectionString = builder.Configuration.GetSection("DataBase")["ConnectionString"]!;
builder.Services.AddHealthChecks()
	.AddNpgSql(connectionString, name: "postgresql");

var openRouterOptions = builder.Configuration.GetSection(nameof(OpenRouterOptions)).Get<OpenRouterOptions>()!;
builder.Services.AddSingleton(openRouterOptions);
var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.MapHealthChecks("/health");
app.AddHangfire();

app.Run();


file sealed class DesignTimeIdentityProvider : IIdentityProvider
{
	public Identity Current => Identity.Anonymous;
	public void Set(Identity identity) { }
}
