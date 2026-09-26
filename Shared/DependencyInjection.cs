using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Shared.OpenRouter;
using Shared.Services;
using Shared.Social.Bluesky;
using Shared.Telegram;
using Shared.TgStat;
using Shared.Video;
using Shared.YouTube;

namespace Shared;

public static class DependencyInjection
{
	/// <summary>
	///     Регистрирует все сервисы из библиотеки Shared
	/// </summary>
	public static IServiceCollection AddShared(this IServiceCollection services)
	{
		services.AddHttpClient();
		services.AddSingleton<TelegramBotManager>();

		services.AddHttpClient(OpenRouterClient.HttpClientName)
			.ConfigurePrimaryHttpMessageHandler(sp =>
			{
				var proxy = sp.GetService<IWebProxy>();
				return new SocketsHttpHandler
				{
					AutomaticDecompression = DecompressionMethods.All,
					PooledConnectionLifetime = TimeSpan.FromMinutes(2),
					Proxy = proxy,
					UseProxy = proxy is not null
				};
			});

		services.AddScoped<IOpenRouterClient, OpenRouterClient>();

		services.AddHttpClient(BlueskyClient.HttpClientName)
			.ConfigurePrimaryHttpMessageHandler(sp =>
			{
				var proxy = sp.GetService<IWebProxy>();
				return new SocketsHttpHandler
				{
					AutomaticDecompression = DecompressionMethods.All,
					PooledConnectionLifetime = TimeSpan.FromMinutes(2),
					Proxy = proxy,
					UseProxy = proxy is not null
				};
			})
			.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(100));

		services.AddScoped<IBlueskyClient, BlueskyClient>();

		services.AddScoped<ITgStatScrapingService, TgStatScrapingService>();
		services.AddScoped<TimePostingService>();
		services.AddScoped<VideoService>();
		services.AddScoped<YouTubeService>();

		return services;
	}
}