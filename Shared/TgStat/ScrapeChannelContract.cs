namespace Shared.TgStat;

/// <summary>
///     Контракт MassTransit для скрейпинга канала с TGStat.
/// </summary>
public sealed class ScrapeChannelContract
{
	public required string Url { get; init; }
}
