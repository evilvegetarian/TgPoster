using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TgPoster.Storage.Config;
using TgPoster.Storage.Data;

namespace TgPoster.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var dataBase = configuration.GetSection(nameof(DataBase)).Get<DataBase>()!;
        services.AddDbContextPool<PosterDbContext>(db => db.UseNpgsql(dataBase.ConnectionString));

        return services;
    }
}