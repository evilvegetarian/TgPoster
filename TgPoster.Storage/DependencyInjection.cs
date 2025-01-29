using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TgPoster.Domain.UseCases.Accounts.SignIn;
using TgPoster.Domain.UseCases.Accounts.SignOn;
using TgPoster.Storage.ConfigModels;
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
        services.AddScoped<ISignInStorage, SignInStorage>();
        services.AddScoped<GuidFactory>();
        return services;
    }
}