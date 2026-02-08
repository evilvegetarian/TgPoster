using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Telegram;
using TgPoster.Storage.ConfigModels;
using TgPoster.Storage.Data;
using TgPoster.Storage.Repositories;

namespace TgPoster.Storage;

public static class DependencyInjection
{
	public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
	{
		var dataBase = configuration.GetSection(nameof(DataBase)).Get<DataBase>()!;
		services.AddDbContext<PosterContext>(db =>
			db.UseNpgsql(dataBase.ConnectionString)
				.EnableSensitiveDataLogging());

		services.AddScoped<GuidFactory>();
		services.AddScoped<ITelegramSessionRepository, TelegramSessionRepository>();
		services.AddScoped<ITelegramAuthRepository, TelegramSessionRepository>();
		services.RegisterStorage();

		return services;
	}

	private static IServiceCollection RegisterStorage(this IServiceCollection services)
	{
		var types = typeof(DependencyInjection).Assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Storage"));

		foreach (var type in types)
		{
			var interfaces = type.GetInterfaces();
			foreach (var inter in interfaces)
			{
				services.Add(new ServiceDescriptor(inter, type, ServiceLifetime.Scoped));
			}
		}

		return services;
	}
}