using System.Reflection;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Monitoring;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain;

public static class DependencyInjection
{
	public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
	{
		services
			.AddMediatR(cfg => cfg
				.AddOpenBehavior(typeof(MonitoringPipelineBehavior<,>))
				.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(DependencyInjection))!));

		var telegramOptions = configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
		services.AddSingleton(telegramOptions);

		var routerOptions = configuration.GetSection(nameof(OpenRouterOptions)).Get<OpenRouterOptions>()!;
		services.AddSingleton(routerOptions);

		var s3Options = configuration.GetSection(nameof(S3Options)).Get<S3Options>()!;
		services.AddSingleton(s3Options);

		services.AddSingleton<IAmazonS3>(_ =>
			new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, new AmazonS3Config
			{
				ServiceURL = s3Options.ServiceUrl,
				ForcePathStyle = true
			}));

		services.AddShared();
		services
			.AddScoped<ITelegramService, TelegramService>()
			.AddScoped<FileService>()
			.AddScoped<TelegramTokenService>()
			.AddSingleton<DomainMetrics>();
		return services;
	}
}

public class S3Options
{
	public required string AccessKey { get; set; }
	public required string SecretKey { get; set; }
	public required string ServiceUrl { get; set; }
	public required string BucketName { get; set; }
}