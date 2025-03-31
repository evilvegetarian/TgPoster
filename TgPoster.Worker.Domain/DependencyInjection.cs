using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TgPoster.Worker.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}