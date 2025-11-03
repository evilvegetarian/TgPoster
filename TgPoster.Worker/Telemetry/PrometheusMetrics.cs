using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace TgPoster.Worker.Telemetry;

public static class PrometheusMetrics
{
	public static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
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
}