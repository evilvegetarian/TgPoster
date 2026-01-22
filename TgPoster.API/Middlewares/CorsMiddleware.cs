using TgPoster.API.Configuration;

namespace TgPoster.API.Middlewares;

/// <summary>
///     Middleware для настройки CORS
/// </summary>
public static class CorsMiddleware
{
	/// <summary>
	///     Добавить CORS политику в приложение
	/// </summary>
	public static void AddCors(this WebApplicationBuilder builder, string corsName)
	{
		var corsConfiguration = builder.Configuration.GetSection(nameof(CorsPolicies)).Get<CorsPolicies>()!;
		builder.Services.AddCors(options =>
		{
			options.AddPolicy(corsName,
				policy =>
				{
					if (builder.Environment.IsDevelopment())
					{
						policy.AllowAnyOrigin()
							.AllowAnyMethod()
							.AllowAnyHeader();
					}
					else
					{
						foreach (var origin in corsConfiguration.AllowedOrigins)
						{
							policy.WithOrigins(origin)
								.AllowAnyMethod()
								.AllowAnyHeader();
						}
					}
				});
		});
	}
}