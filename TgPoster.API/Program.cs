using Auth;
using TgPoster.API.Middlewares;
using TgPoster.Domain;
using TgPoster.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddStorage(builder.Configuration)
    .AddDomain()
    .AddAuth(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();

public partial class Program { }