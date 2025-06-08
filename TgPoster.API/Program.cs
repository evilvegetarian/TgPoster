using MassTransit;
using Microsoft.AspNetCore.StaticFiles;
using Security;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.Grafana.Loki;
using Shared.Contracts;
using TgPoster.API.ConfigModels;
using TgPoster.API.Domain;
using TgPoster.API.Middlewares;
using TgPoster.Storage;

var builder = WebApplication.CreateBuilder(args);

var logger = builder.Configuration.GetSection(nameof(Logger)).Get<Logger>()!;
var serilog = new LoggerConfiguration()
    .MinimumLevel.Information()
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

builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<FileExtensionContentTypeProvider>();
builder.Services
    .AddStorage(builder.Configuration)
    .AddDomain(builder.Configuration)
    .AddSecurity(builder.Configuration);

var dataBase = builder.Configuration.GetSection(nameof(DataBase)).Get<DataBase>()!;

builder.Services.AddMassTransit(x =>
{
    x.UsingPostgres();
    x.ConfigureMassTransient(dataBase.ConnectionString);
});
var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.UseHttpsRedirection();
app.Run();

public partial class Program
{
}