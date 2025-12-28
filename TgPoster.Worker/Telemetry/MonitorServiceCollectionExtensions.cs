using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TgPoster.Worker.Configuration;

namespace TgPoster.Worker.Telemetry;

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
				.ConfigureResource(r => r.AddService("TgPoster.Worker"))
				//.AddSource(DomainMetrics.ApplicationName)
				.AddAspNetCoreInstrumentation(options =>
				{
					options.Filter += context =>
						!context.Request.Path.Value!.Contains("metrics",
							StringComparison.InvariantCultureIgnoreCase);
					options.EnrichWithHttpResponse = (activity, response) =>
						activity.AddTag("error", response.StatusCode >= 400);
				})
				.AddHangfireInstrumentation()
				.AddHttpClientInstrumentation()
				.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
				.AddConsoleExporter()
				.AddOtlpExporter(cfg =>
				{
					cfg.Endpoint = new Uri(configuration.Url);
					cfg.Protocol = OtlpExportProtocol.HttpProtobuf;
				})
			);

		return services;
	}

	public static IServiceCollection AddMonitors(
		this IServiceCollection services,
		TracingConfiguration configuration
	) =>
		services
			.AddMetrics()
			.AddTracing(configuration);
}