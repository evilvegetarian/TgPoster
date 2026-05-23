using Microsoft.Extensions.DependencyInjection;
using Shared.OpenRouter;
using Shared.Services;
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

		services.AddScoped<IOpenRouterClient, OpenRouterClient>();
		services.AddScoped<ITgStatScrapingService, TgStatScrapingService>();
		services.AddScoped<TimePostingService>();
		services.AddScoped<VideoService>();
		services.AddScoped<YouTubeService>();

		return services;
	}
}