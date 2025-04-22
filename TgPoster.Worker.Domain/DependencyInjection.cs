using Contracts;
using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

namespace TgPoster.Worker.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransient(configuration);
        
        services.AddHangfire(configuration =>
        {
            configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage();
        });
        services.AddHangfireServer();
        services.AddScoped<SenderMessageWorker>();
        services.AddScoped<ParseChannelWorker>();
        
        return services;
    }


    public static void AddMassTransient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ParseChannelConsumer>();

            x.UsingPostgres((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
            var dataBase = configuration.GetSection(nameof(DataBase)).Get<DataBase>()!;
            x.ConfigureMassTransient(dataBase.ConnectionString);
            x.AddPostgresMigrationHostedService();
        });
    }

    public static IHost AddHangfire(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<SenderMessageWorker>(
            "process-sender-message-job",
            worker => worker.ProcessMessagesAsync(),
            Cron.Minutely());

        recurringJobManager.AddOrUpdate<ParseChannelWorker>(
            "process-parse-channel-job",
            worker => worker.ProcessMessagesAsync(),
            Cron.Daily());
        return host;
    }
    
}

internal class DataBase
{
    public required string ConnectionString { get; init; }
}