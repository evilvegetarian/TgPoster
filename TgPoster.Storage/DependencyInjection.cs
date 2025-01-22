using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TgPoster.Domain.UseCases.SignOn;
using TgPoster.Storage.Config;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var dataBase = configuration.GetSection(nameof(DataBase)).Get<DataBase>()!;
        services.AddDbContextPool<PosterContext>(db => db.UseNpgsql(dataBase.ConnectionString));
        
        services.AddScoped<ISignOnStorage, SignOnStorage>();
        services.AddScoped<IGuidFactory, GuidFactory>();
        return services;
    }
}