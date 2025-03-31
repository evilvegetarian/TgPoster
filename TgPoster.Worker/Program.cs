using Hangfire;
using Hangfire.MemoryStorage;
using Security;
using Security.Interfaces;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.Storage;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

var builder = Host.CreateApplicationBuilder(args);

var telegramOptions = builder.Configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
builder.Services.AddSingleton(telegramOptions);
builder.Services.AddScoped<SenderMessageWorker>();
builder.Services.AddHangfire(configuration =>
{
    configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage();
});
builder.Services.AddHangfireServer();
builder.Services.AddScoped<ICryptoAES, CryptoAES>();
builder.Services
    .AddStorage(builder.Configuration);
var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<SenderMessageWorker>(
        "process-messages-job",
        worker => worker.ProcessMessagesAsync(),
        Cron.Minutely());
}

host.Run();