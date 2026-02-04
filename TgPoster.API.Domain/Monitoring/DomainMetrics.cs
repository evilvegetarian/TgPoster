using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TgPoster.API.Domain.Monitoring;

public class DomainMetrics(IMeterFactory meterFactory)
{
	public const string ApplicationName = "TgPoster.API.Domain";
	internal static readonly ActivitySource ActivitySource = new(ApplicationName);
	private readonly Meter meter = meterFactory.Create(ApplicationName);

	public void IncrementCount(string? name, int value, IDictionary<string, object?>? additionalTags = null)
	{
		if (name is null) return;
		var counter = meter.CreateCounter<int>(name);
		counter.Add(value, additionalTags?.ToArray() ?? ReadOnlySpan<KeyValuePair<string, object?>>.Empty);
	}

	public static IDictionary<string, object?> ResultTags(bool success) =>
		new Dictionary<string, object?>
		{
			["success"] = success
		};
}