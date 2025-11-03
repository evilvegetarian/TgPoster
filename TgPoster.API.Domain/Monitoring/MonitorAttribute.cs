namespace TgPoster.API.Domain.Monitoring;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class MonitorAttribute(string? counterName = null) : Attribute
{
	public string? CounterName { get; } = counterName;
}