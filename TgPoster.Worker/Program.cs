using Security;
using Security.Interfaces;
using TgPoster.Storage;
using TgPoster.Worker.Domain;
using TgPoster.Worker.Domain.ConfigModels;

var builder = Host.CreateApplicationBuilder(args);

var telegramOptions = builder.Configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
builder.Services.AddSingleton(telegramOptions);

builder.Services.AddScoped<ICryptoAES, CryptoAES>();
builder.Services
    .AddDomain(builder.Configuration)
    .AddStorage(builder.Configuration);
var host = builder.Build();
host.AddHangfire();
host.Run();
