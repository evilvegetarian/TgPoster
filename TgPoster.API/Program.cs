using MassTransit;
using Microsoft.AspNetCore.StaticFiles;
using Security;
using Shared.Contracts;
using TgPoster.API.Domain;
using TgPoster.API.Middlewares;
using TgPoster.Storage;

var builder = WebApplication.CreateBuilder(args);
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

app.Run();

public partial class Program
{
}

internal class DataBase
{
    public required string ConnectionString { get; init; }
}