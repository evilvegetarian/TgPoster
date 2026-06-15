namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

public sealed record AddRepostDestinationResponse
{
	/// <summary>
	///     Id созданного целевого канала.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Id связанной записи в Discover.
	/// </summary>
	public required Guid DiscoveredChannelId { get; init; }
}
