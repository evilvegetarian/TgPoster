using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Services;

namespace TgPoster.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddMediatR(cfg => cfg
                .RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(DependencyInjection))!));

        services.Configure<TelegramOptions>(configuration.GetSection(nameof(TelegramOptions)));
        services.AddScoped<TelegramService>();
        services.AddScoped<VideoService>();
        services.AddScoped<FileService>();
        services.AddScoped<TimePostingService>();
        services.AddScoped<TelegramTokenService>();
        return services;
    }
}