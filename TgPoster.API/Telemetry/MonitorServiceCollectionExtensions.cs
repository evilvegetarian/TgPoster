using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TgPoster.API.ConfigModels;

namespace TgPoster.API.Telemetry;

/// <summary>
/// 
/// </summary>
public static class MonitorServiceCollectionExtensions
{
	private static IServiceCollection AddMetrics(this IServiceCollection services)
	{
		services.AddOpenTelemetry()
			.ConfigureResource(resourceBuilder => resourceBuilder
				.AddAttributes(new Dictionary<string, object>
				{
					["EnvironmentName"] = Environment.MachineName
				}))
			.WithMetrics(meterProviderBuilder => meterProviderBuilder
				.AddAspNetCoreInstrumentation()
				.AddHttpClientInstrumentation()
				.AddRuntimeInstrumentation()
				.AddProcessInstrumentation()
				.AddPrometheusExporter()
			);
		return services;
	}

	private static IServiceCollection AddTracing(this IServiceCollection services, TracingConfiguration configuration)
	{
		services
			.AddOpenTelemetry()
			.WithTracing(builder => builder
				.ConfigureResource(r => r.AddService("TgPoster.API"))
				//.AddSource(DomainMetrics.ApplicationName)
				.AddAspNetCoreInstrumentation(options =>
				{
					options.Filter += context =>
						!context.Request.Path.Value!.Contains("metrics",
							StringComparison.InvariantCultureIgnoreCase)
						&& !context.Request.Path.Value!.Contains("swagger",
							StringComparison.InvariantCultureIgnoreCase);
					options.EnrichWithHttpResponse = (activity, response) =>
						activity.AddTag("error", response.StatusCode >= 400);
				})
				.AddHangfireInstrumentation()
				.AddHttpClientInstrumentation()
				.AddHttpClientInstrumentation()
				.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
				.AddConsoleExporter()
				.AddOtlpExporter(cfg =>
				{
					cfg.Endpoint = new Uri(configuration.Url);
					cfg.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
				})
			);

		return services;
	}

	public static IServiceCollection AddMonitors(this IServiceCollection services, TracingConfiguration configuration)
	{
		return services
			.AddMetrics()
			.AddTracing(configuration);
	}
}