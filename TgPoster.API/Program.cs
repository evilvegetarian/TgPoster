using System.Reflection;
using MassTransit;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.OpenApi.Models;
using Security;
using Serilog;
using Serilog.Events;
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

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n"
                      + "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n"
                      + "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

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