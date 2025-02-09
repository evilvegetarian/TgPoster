using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgPoster.Domain.ConfigModels;

namespace TgPoster.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddMediatR(cfg => cfg
                .RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(DependencyInjection))!));

        services.Configure<TelegramOptions>(configuration.GetSection(nameof(TelegramOptions)));

        return services;
    }
}