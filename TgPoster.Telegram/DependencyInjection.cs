using Microsoft.Extensions.DependencyInjection;
using TgPoster.Telegram.Internal;

namespace TgPoster.Telegram;

public static class DependencyInjection
{
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
		services.AddScoped<ITelegramChatService, TelegramChatService>();
		services.AddScoped<ITelegramMessageService, TelegramMessageService>();
		services.AddScoped<ITelegramPublicLookupService, TelegramPublicLookupService>();

		return services;
	}
}
