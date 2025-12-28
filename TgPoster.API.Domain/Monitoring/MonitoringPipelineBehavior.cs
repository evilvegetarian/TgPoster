using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TgPoster.API.Domain.Monitoring;

internal class MonitoringPipelineBehavior<TRequest, TResponse>(
	DomainMetrics metrics,
	ILogger<MonitoringPipelineBehavior<TRequest, TResponse>> logger)
	: IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken
	)
	{
		// var monitorAttribute = request.GetType().GetCustomAttribute<MonitorAttribute>();
		//
		// if (monitorAttribute is null)
		// {
		// 	return await next.Invoke();
		// }

		using var activity = DomainMetrics.ActivitySource.StartActivity("usecase");
		activity?.AddTag("app.use_case", request.GetType().Name);
		var counterName = /*monitorAttribute.CounterName ??*/ request.GetType().Name;

		try
		{
			var result = await next.Invoke();

			logger.LogDebug("UseCase {UseCaseName} handled successfully.", request.GetType().Name);
			metrics.IncrementCount(counterName, 1, DomainMetrics.ResultTags(true));
			activity?.SetStatus(ActivityStatusCode.Ok);

			return result;
		}
		catch (Exception e)
		{
			logger.LogError(e, "Unhandled error caught while handling UseCase {UseCaseName}", request.GetType().Name);
			metrics.IncrementCount(counterName, 1, DomainMetrics.ResultTags(false));
			activity?.SetStatus(ActivityStatusCode.Error, e.Message);
			activity?.AddTag("exception.type", e.GetType().FullName);

			throw;
		}
	}
}