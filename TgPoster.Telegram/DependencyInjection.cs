using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Configuration;
using TgPoster.Telegram.Internal;

namespace TgPoster.Telegram;

public static class DependencyInjection
{
	/// <summary>
	///     Имя HttpClient для запросов к публичным страницам t.me
	/// </summary>
	internal const string PublicLookupClient = "tme-lookup";

	/// <summary>
	///     Регистрирует все сервисы библиотеки TgPoster.Telegram (WTelegram-клиент)
	/// </summary>
	public static IServiceCollection AddTgPosterTelegram(this IServiceCollection services)
	{
		services.AddSingleton<TelegramClientManager>();
		services.AddSingleton<SessionDataDebouncer>();
		services.AddScoped<TelegramAuthService>();
		services.AddScoped<ITelegramAuthService>(sp => sp.GetRequiredService<TelegramAuthService>());
		services.AddScoped<ITelegramClientResolver>(sp => sp.GetRequiredService<TelegramAuthService>());
		services.AddScoped<ITelegramChatService, Internal.TelegramChatService>();
		services.AddScoped<ITelegramMessageService, TelegramMessageService>();
		services.AddScoped<ITelegramPublicLookupService, TelegramPublicLookupService>();

		services.AddOptions<TelegramPublicLookupOptions>().BindConfiguration("TelegramPublicLookup");
		services.AddSingleton<DbActiveHttpProxy>();
		services.AddSingleton<IWebProxy>(sp => sp.GetRequiredService<DbActiveHttpProxy>());

		services.AddHttpClient(PublicLookupClient)
			.ConfigurePrimaryHttpMessageHandler(sp =>
			{
				var o = sp.GetRequiredService<IOptions<TelegramPublicLookupOptions>>().Value;
				return new SocketsHttpHandler
				{
					AutomaticDecompression = DecompressionMethods.All,
					ConnectTimeout = TimeSpan.FromSeconds(o.ConnectTimeoutSeconds),
					PooledConnectionLifetime = TimeSpan.FromMinutes(2),
					Proxy = sp.GetRequiredService<DbActiveHttpProxy>(),
					UseProxy = true
				};
			});

		return services;
	}
}
