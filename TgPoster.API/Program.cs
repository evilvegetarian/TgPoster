using System.Reflection;
using MassTransit;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.OpenApi.Models;
using Security;
using Shared.Contracts;
using TgPoster.API.ConfigModels;
using TgPoster.API.Domain;
using TgPoster.API.Middlewares;
using TgPoster.API.Telemetry;
using TgPoster.Storage;

var builder = WebApplication.CreateBuilder(args);
const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.AddLogging();
builder.AddCors(MyAllowSpecificOrigins);

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
builder.Services.AddPrometheusMetrics();
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
app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();

public partial class Program
{
}