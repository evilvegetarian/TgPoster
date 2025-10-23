using TgPoster.API.ConfigModels;

namespace TgPoster.API.Middlewares;

public static class CorsMiddleware
{
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