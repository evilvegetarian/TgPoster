using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Instrumentation.EntityFrameworkCore.Implementation;
using TgPoster.API.Domain.Monitoring;

namespace TgPoster.API.Telemetry;

/// <summary>
/// Метрики через Prometheus
/// </summary>
public static class PrometheusMetrics
{
	/// <summary>
	/// Добавляет метрики
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
	{
		services.AddOpenTelemetry()
			//.ConfigureResource(resourceBuilder => resourceBuilder
			//	.AddAttributes(new Dictionary<string, object>
			//	{
			//		["EnvironmentName"] = Environment.MachineName
			//	}))
			.WithMetrics(meterProviderBuilder => meterProviderBuilder
					.AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
					.AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
					.AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
					.AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
					.AddMeter(DomainMetrics.ApplicationName)
					.AddPrometheusExporter() // add OpenTelemetry.Exporter.Prometheus.AspNetCore nuget package
			)
			
			;
		return services;
	}
}