using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TgPoster.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services
            .AddMediatR(cfg => cfg
                .RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(DependencyInjection))!));

        return services;
    }
}